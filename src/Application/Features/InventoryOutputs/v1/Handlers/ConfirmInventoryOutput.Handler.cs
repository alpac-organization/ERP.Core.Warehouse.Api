using System.Globalization;
using System.Net;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Notification.Application.Commons.Interfaces;
using ERP.Core.Notification.Domain.Entities;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Commands;

using OutputEntity = ERP.Core.Database.Domain.Entities.Inventory.InventoryOutputs;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Handlers
{
    public class ConfirmInventoryOutputHandler(
        IUnitOfWork unitOfWork, IErrorManager errorManager,
        IEmailNotificationService emailService,
        ILogger<ConfirmInventoryOutputHandler> logger)
        : BaseValidatorHandler<ConfirmInventoryOutputCommand, Unit>(unitOfWork, errorManager)
    {
        public override async Task<Unit> Handle(ConfirmInventoryOutputCommand request, CancellationToken ct)
        {
            var (access, output, error) = await ValidateAndLoadOutputAsync(request, ct);
            if (error is not null) return error ?? Unit.Value;

            var now = DateTime.UtcNow;
            var lowStockItems = new List<(string Name, string Code, decimal Available, decimal Minimum)>();

            foreach (var item in output!.InventoryOutputsItems)
            {
                var lowStock = await ProcessItemAsync(item, output, request.UserId, now, ct);
                if (lowStock.HasValue)
                    lowStockItems.Add(lowStock.Value);
            }

            await SendLowStockNotificationAsync(lowStockItems, output.Code, access.User.Email, ct);

            output.Status = InventoryOutputsStatus.Dispatched;
            output.DispatchedAt = now;
            output.DispatchedByUserId = request.UserId;

            await _unitOfWork.InventoryOutputs.UpdateAsync(output);
            await _unitOfWork.SaveChangesAsync(ct);

            return Unit.Value;
        }

        private async Task<(AccessValidationResult<Unit> access, OutputEntity? output, Unit? error)> ValidateAndLoadOutputAsync(
            ConfirmInventoryOutputCommand request, CancellationToken ct)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
            if (!access.IsSuccess)
                return (access, null, access.ErrorResponse);

            if (access.Role?.RoleType is not (RoleType.Administrator or RoleType.Supervisor))
                return (access, null, _errorManager.ThrowUnauthorized<Unit>("No tiene acceso para realizar esta acción", "ERP:INVALID_ACCESS"));

            var output = await _unitOfWork.InventoryOutputs.Entities
                .Include(o => o.InventoryOutputsItems)
                    .ThenInclude(i => i.ProductStock)
                        .ThenInclude(ps => ps.Product)
                .FirstOrDefaultAsync(o => o.Id == request.InventoryOutputId && o.DeletedAt == null, ct);

            if (output is null)
                return (access, null, _errorManager.ThrowNotFound<Unit>("La salida de inventario no fue encontrada.", "ERP:INVENTORY_OUTPUT_NOT_FOUND"));

            if (output.Status != InventoryOutputsStatus.InProgress)
                return (access, null, _errorManager.ThrowBadRequest<Unit>("La salida no está en proceso de despacho.", "ERP:INVENTORY_OUTPUT_NOT_IN_PROGRESS"));

            return (access, output, null);
        }

        private async Task<(string Name, string Code, decimal Available, decimal Minimum)?> ProcessItemAsync(
            InventoryOutputsItem item, OutputEntity output, Guid userId, DateTime now, CancellationToken ct)
        {
            var stock = item.ProductStock;
            if (stock is null) return null;

            var previousUnits = stock.AvailableQuantity;
            var previousCost = stock.WeightedAverageCost;

            var dispatched = item.DispatchedQuantity;

            stock.AvailableQuantity = previousUnits - dispatched;

            if (stock.AvailableQuantity <= 0)
                stock.Status = ProductStockStatus.Empty;
            else if (stock.AvailableQuantity <= stock.MinimumStock)
                stock.Status = ProductStockStatus.LowBalance;
            else
                stock.Status = ProductStockStatus.Stable;

            stock.WeightedAverageCost = previousCost;

            await _unitOfWork.ProductStocks.UpdateAsync(stock);

            var kardex = InventoryOutputMapper.ToKardexEntity(item, stock, output, userId, now);
            await _unitOfWork.Kardex.RegisterKardexMovement(kardex);

            if (stock.Status is ProductStockStatus.LowBalance or ProductStockStatus.Empty)
            {
                return (
                    stock.Product?.ProductName ?? stock.Product?.Code ?? "Producto",
                    stock.Product?.Code ?? "-",
                    stock.AvailableQuantity,
                    stock.MinimumStock);
            }

            return null;
        }

        private async Task SendLowStockNotificationAsync(
            List<(string Name, string Code, decimal Available, decimal Minimum)> lowStockItems,
            string outputCode, string? userEmail, CancellationToken ct)
        {
            if (lowStockItems.Count == 0) return;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                logger.LogError("No se pudo enviar alerta de stock bajo para la salida {OutputCode}: el usuario no tiene correo configurado.", outputCode);
                return;
            }

            var message = new EmailMessage
            {
                Subject = "Alerta de stock bajo - Salida " + outputCode,
                Body = BuildLowStockEmailBody(lowStockItems, outputCode, DateTime.Now),
                IsHtml = true
            };
            message.AddTo(userEmail);

            var sent = await emailService.SendAsync(message, ct);
            if (sent)
                logger.LogInformation("Alerta de stock bajo enviada a {Email} para la salida {OutputCode}.", userEmail, outputCode);
            else
                logger.LogError("Fallo el envio de alerta de stock bajo a {Email} para la salida {OutputCode}.", userEmail, outputCode);
        }

        private static string BuildLowStockEmailBody(
            List<(string Name, string Code, decimal Available, decimal Minimum)> items,
            string outputCode, DateTime confirmedAt)
        {
            static string Enc(string value) => WebUtility.HtmlEncode(value);
            static string Num(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);

            const string thStyle =
                "padding:10px 12px;font-size:13px;font-weight:bold;color:#52525b;border-bottom:1px solid #d4d4d8;";
            const string tdStyle =
                "padding:12px;font-size:14px;color:#18181b;border-bottom:1px solid #e4e4e7;";

            var rows = new StringBuilder();
            foreach (var item in items)
            {
                var missing = Math.Max(0m, item.Minimum - item.Available);

                rows.Append("<tr>");
                rows.Append("<td style=\"" + tdStyle + "\">" + Enc(item.Name) + "</td>");
                rows.Append("<td style=\"" + tdStyle + "color:#52525b;\">" + Enc(item.Code) + "</td>");
                rows.Append("<td align=\"right\" style=\"" + tdStyle + "font-weight:bold;color:#b91c1c;\">" + Num(item.Available) + "</td>");
                rows.Append("<td align=\"right\" style=\"" + tdStyle + "\">" + Num(item.Minimum) + "</td>");
                rows.Append("<td align=\"right\" bgcolor=\"#fee2e2\" style=\"" + tdStyle + "font-weight:bold;color:#b91c1c;background-color:#fee2e2;\">" + Num(missing) + "</td>");
                rows.Append("</tr>");
            }

            var body = "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"#f4f4f5\" style=\"background-color:#f4f4f5;\">";
            body += "<tr><td align=\"center\" style=\"padding:24px 12px;\">";
            body += "<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"#ffffff\" style=\"width:100%;max-width:600px;background-color:#ffffff;border:1px solid #e4e4e7;font-family:Segoe UI,Arial,sans-serif;\">";
            body += "<tr><td bgcolor=\"#fef3c7\" style=\"background-color:#fef3c7;padding:14px 20px;font-size:16px;font-weight:bold;color:#92400e;\">⚠️ Alerta de stock bajo</td></tr>";
            body += "<tr><td style=\"padding:20px;\">";
            body += "<p style=\"margin:0 0 4px 0;font-size:15px;line-height:1.6;color:#18181b;\">Se detecto stock por debajo del minimo tras confirmar la salida <b>" + Enc(outputCode) + "</b>.</p>";
            body += "<p style=\"margin:0 0 18px 0;font-size:13px;color:#71717a;\">Confirmada el " + confirmedAt.ToString("dd/MM/yyyy") + " a las " + confirmedAt.ToString("HH:mm") + "</p>";
            body += "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse;\">";
            body += "<thead><tr bgcolor=\"#f4f4f5\" style=\"background-color:#f4f4f5;\">";
            body += "<th align=\"left\" style=\"" + thStyle + "\">Producto</th>";
            body += "<th align=\"left\" style=\"" + thStyle + "\">Codigo</th>";
            body += "<th align=\"right\" style=\"" + thStyle + "\">Restante</th>";
            body += "<th align=\"right\" style=\"" + thStyle + "\">Minimo</th>";
            body += "<th align=\"right\" style=\"" + thStyle + "\">Faltante</th>";
            body += "</tr></thead>";
            body += "<tbody>" + rows.ToString() + "</tbody>";
            body += "</table></td></tr></table></td></tr></table>";

            return body;
        }
    }
}