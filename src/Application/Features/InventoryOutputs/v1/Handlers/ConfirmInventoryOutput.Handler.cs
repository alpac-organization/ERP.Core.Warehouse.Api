using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Notification.Domain.Entities;
using ERP.Core.Notification.Application.Commons.Interfaces;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Helpers.Inventory;
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

            // Calcular cantidades por stock (despacho completo)
            var requestedByStock = BuildRequestedQuantities(output!);

            foreach (var (stockId, dispatched) in requestedByStock)
            {
                var stock = await _unitOfWork.ProductStocks.Entities
                    .Include(ps => ps.Product)
                    .FirstOrDefaultAsync(ps => ps.Id == stockId && ps.DeletedAt == null, ct);

                if (stock is null) continue;

                var lowStock = await ProcessStockAsync(stock, dispatched, output!, request.UserId, now, ct);
                if (lowStock.HasValue)
                    lowStockItems.Add(lowStock.Value);
            }

            await SendLowStockNotificationAsync(lowStockItems, output!.Code, access.User.Email, ct);

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
                .Include(o => o.SupplyRequest)
                    .ThenInclude(s => s.SupplyRequestItems)
                .Include(o => o.FuelRequest)
                .FirstOrDefaultAsync(o => o.Id == request.InventoryOutputId && o.DeletedAt == null, ct);

            if (output is null)
                return (access, null, _errorManager.ThrowNotFound<Unit>("La salida de inventario no fue encontrada.", "ERP:INVENTORY_OUTPUT_NOT_FOUND"));

            if (output.Status != InventoryOutputsStatus.InProgress)
                return (access, null, _errorManager.ThrowBadRequest<Unit>("La salida no está en proceso de despacho.", "ERP:INVENTORY_OUTPUT_NOT_IN_PROGRESS"));

            return (access, output, null);
        }

        private async Task<(string Name, string Code, decimal Available, decimal Minimum)?> ProcessStockAsync(
            ProductStock stock, decimal dispatched, OutputEntity output, Guid userId, DateTime now, CancellationToken ct)
        {
            if (stock is null) return null;

            var previousUnits = stock.AvailableQuantity + dispatched;
            var previousCost = stock.WeightedAverageCost;

            // Stock ya fue reservado al crear la solicitud; aquí solo se registra el kardex.
            // No se modifica AvailableQuantity.

            stock.WeightedAverageCost = previousCost;

            var kardex = InventoryOutputMapper.ToKardexEntity(stock, dispatched, output, userId, now, previousUnits, previousCost);
            await _unitOfWork.Kardex.RegisterKardexMovement(kardex);

            if (stock.AvailableQuantity <= 0)
                stock.Status = ProductStockStatus.Empty;
            else if (stock.AvailableQuantity <= stock.MinimumStock)
                stock.Status = ProductStockStatus.LowBalance;
            else
                stock.Status = ProductStockStatus.Stable;

            await _unitOfWork.ProductStocks.UpdateAsync(stock);

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

        private static Dictionary<Guid, decimal> BuildRequestedQuantities(OutputEntity output)
        {
            var requestedByStock = new Dictionary<Guid, decimal>();

            if (output.SupplyRequest is not null)
            {
                foreach (var item in output.SupplyRequest.SupplyRequestItems)
                {
                    if (!item.IsActive) continue;
                    if (requestedByStock.TryGetValue(item.ProductStockId, out var current))
                        requestedByStock[item.ProductStockId] = current + item.Amount;
                    else
                        requestedByStock[item.ProductStockId] = item.Amount;
                }
            }

            if (output.FuelRequest is not null)
            {
                var stockId = output.FuelRequest.ProductStockId;
                var qty = output.FuelRequest.RequestedQuantity;
                if (requestedByStock.TryGetValue(stockId, out var current))
                    requestedByStock[stockId] = current + qty;
                else
                    requestedByStock[stockId] = qty;
            }

            return requestedByStock;
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
                Body = LowStockNotificationHelper.BuildEmailBody(lowStockItems, outputCode, DateTime.Now),
                IsHtml = true
            };
            message.AddTo(userEmail);

            var sent = await emailService.SendAsync(message, ct);
            if (sent)
                logger.LogInformation("Alerta de stock bajo enviada a {Email} para la salida {OutputCode}.", userEmail, outputCode);
            else
                logger.LogError("Fallo el envio de alerta de stock bajo a {Email} para la salida {OutputCode}.", userEmail, outputCode);
        }

    }
}