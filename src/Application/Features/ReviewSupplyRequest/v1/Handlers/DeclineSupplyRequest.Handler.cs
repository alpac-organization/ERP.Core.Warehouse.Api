using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.ReviewSupplyRequest.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReviewSupplyRequest.v1.Handlers;

public class DeclineSupplyRequestHandler(
    IUnitOfWork _unitOfWork, IErrorManager _errorManager, ILogger<DeclineSupplyRequestHandler> logger)
    : BaseValidatorHandler<DeclineSupplyRequestCommand, Unit>(_unitOfWork, _errorManager)
{
    public override async Task<Unit> Handle(DeclineSupplyRequestCommand request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var allowedArea = await _unitOfWork.InventoryAreasPermissions.Entities
            .AsNoTracking()
            .Where(a => a.WorkAreaId == access.Profile.AreaId && a.IsActive)
            .FirstOrDefaultAsync(ct);

        if (allowedArea is null || access.Role?.RoleType is not (RoleType.Supervisor or RoleType.Administrator))
            return _errorManager.ThrowBadRequest<Unit>(
                "No tienes permiso para esta acción.", "ERP:USER_NOT_AUTOIZED");

        var supplyRequest = await _unitOfWork.SupplyRequests.Entities
            .FirstOrDefaultAsync(sr => sr.Id == request.SupplyRequestId && sr.DeletedAt == null, ct);

        if (supplyRequest is null)
            return _errorManager.ThrowNotFound<Unit>(
                "La solicitud de insumos no fue encontrada.", "ERP:SUPPLY_REQUEST_NOT_FOUND");

        if (supplyRequest.OutflowStatus != OutflowStatus.PendingReview)
            return _errorManager.ThrowBadRequest<Unit>(
                "La solicitud de insumos no está en pendiente de revisión.", "ERP:SUPPLY_REQUEST_NOT_PENDING_REVIEW");

        logger.LogInformation("📝 Rechazando solicitud de insumos.");

        try
        {
            supplyRequest.OutflowStatus = OutflowStatus.Annuled;
            await _unitOfWork.SupplyRequests.UpdateAsync(supplyRequest);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error de persistencia al rechazar la solicitud de insumos.");
            return _errorManager.ThrowBadRequest<Unit>(
                "No se pudo rechazar la solicitud de insumos.", "ERP:SUPPLY_REQUEST_DECLINE_PERSISTENCE_ERROR");
        }

        return Unit.Value;
    }
}