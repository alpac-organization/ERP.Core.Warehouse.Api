using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Notification.Domain.Entities;
using ERP.Core.Notification.Application.Commons.Interfaces;

using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Helpers.Inventory;

using OutputEntity = ERP.Core.Database.Domain.Entities.Inventory.InventoryOutputs;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Handlers
{
    public class RejectInventoryOutputHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IEmailNotificationService emailService,
        ILogger<RejectInventoryOutputHandler> logger)
        : BaseValidatorHandler<RejectInventoryOutputCommand, Unit>(unitOfWork, errorManager)
    {
        public override async Task<Unit> Handle(RejectInventoryOutputCommand request, CancellationToken ct)
        {
            var (access, output, error) = await ValidateAndLoadOutputAsync(request, ct);
            if (error is not null) return error ?? Unit.Value;

            var now = DateTime.UtcNow;
            var justification = string.IsNullOrWhiteSpace(request.DispatchedJustification)
                ? "Rechazado"
                : request.DispatchedJustification.Trim();

            var revertedByStock = new Dictionary<Guid, decimal>();

            if (output!.SupplyRequest is not null)
            {
                foreach (var group in output.SupplyRequest.SupplyRequestItems
                    .GroupBy(i => i.ProductStockId))
                {
                    revertedByStock[group.Key] = group.Sum(i => i.Amount);
                }
            }

            if (output.FuelRequest is not null)
            {
                revertedByStock[output.FuelRequest.ProductStockId] =
                    output.FuelRequest.RequestedQuantity;
            }

            await StockReservationHelper.RevertStockAsync(_unitOfWork, revertedByStock, ct);

            RejectOutput(output, justification, request.UserId, now);

            var emails = await GetRecipientEmailsAsync(output!, ct);

            await SendRejectionNotificationAsync(emails, output!, justification, now, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Unit.Value;
        }

        private async Task<(AccessValidationResult<Unit> access, OutputEntity? output, Unit? error)> ValidateAndLoadOutputAsync(
            RejectInventoryOutputCommand request, CancellationToken ct)
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

            if (output.Status != InventoryOutputsStatus.OutputPending)
                return (access, null, _errorManager.ThrowBadRequest<Unit>("Solo se puede rechazar una salida pendiente.", "ERP:INVENTORY_OUTPUT_NOT_PENDING"));

            return (access, output, null);
        }

        private void RejectOutput(OutputEntity output, string justification, Guid userId, DateTime now)
        {
            output.Status = InventoryOutputsStatus.Rejected;
            output.DispatchedJustification = justification;
            output.DispatchedAt = now;
            output.DispatchedByUserId = userId;

            _unitOfWork.InventoryOutputs.UpdateAsync(output);
        }

        private async Task<List<string>> GetRecipientEmailsAsync(OutputEntity output, CancellationToken ct)
        {
            var userIds = new List<Guid>();
            if (output.SupplyRequest is not null)
            {
                userIds.Add(output.SupplyRequest.RequestedByUserId);
                if (output.SupplyRequest.ApprovedByUserChiefId.HasValue)
                    userIds.Add(output.SupplyRequest.ApprovedByUserChiefId.Value);
            }
            if (output.FuelRequest is not null)
            {
                userIds.Add(output.FuelRequest.RequestedByUserId);
                if (output.FuelRequest.ApprovedByUserChiefId.HasValue)
                    userIds.Add(output.FuelRequest.ApprovedByUserChiefId.Value);
            }

            var distinctIds = userIds.Distinct().ToList();
            if (distinctIds.Count == 0) return [];

            var users = await _unitOfWork.Users.Entities
                .Where(u => distinctIds.Contains(u.Id))
                .ToListAsync(ct);

            return [.. users
                .Where(u => !string.IsNullOrWhiteSpace(u.Email))
                .Select(u => u.Email!)
                .Distinct()];
        }

        private async Task SendRejectionNotificationAsync(
            List<string> emails, OutputEntity output, string justification, DateTime now, CancellationToken ct)
        {
            var requestCode = output.SupplyRequest?.Code ?? output.FuelRequest?.Code ?? "-";

            if (emails.Count == 0)
            {
                logger.LogError("No se pudo enviar notificacion de rechazo para la salida {OutputCode}: ningun destinatario tiene correo.", output.Code);
                return;
            }

            var body = "<p>La salida de inventario <b>" + output.Code + "</b> fue rechazada.</p>" +
                "<p><b>Solicitud asociada:</b> " + requestCode + "</p>" +
                "<p><b>Motivo:</b> " + justification + "</p>" +
                "<p><b>Fecha:</b> " + now.ToString("dd/MM/yyyy HH:mm") + "</p>";

            var message = new EmailMessage
            {
                Subject = "Salida de inventario rechazada - " + output.Code,
                Body = body,
                IsHtml = true
            };
            foreach (var email in emails)
                message.AddTo(email);

            var sent = await emailService.SendAsync(message, ct);
            if (sent)
                logger.LogInformation("Notificacion de rechazo enviada para la salida {OutputCode}.", output.Code);
            else
                logger.LogError("Fallo el envio de notificacion de rechazo para la salida {OutputCode}.", output.Code);
        }
    }
}