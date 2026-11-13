using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Helpers.Inventory;
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

        var areaError = await SupplyRequestSharedHelper.ValidateInventoryAreaAsync(
            _unitOfWork, _errorManager, access.Profile.AreaId, ct);

        if (areaError is not null)
            return areaError.Value;

        if (access.Role?.RoleType is not (RoleType.Supervisor or RoleType.Administrator))
            return _errorManager.ThrowBadRequest<Unit>(
                "No tienes permiso para esta acción.", "ERP:USER_NOT_AUTOIZED");

        if (IsFuelRequest(request.Code))
            return await DeclineFuelRequestAsync(request, access, ct);

        return await DeclineSupplyRequestAsync(request, access, ct);
    }

    private static bool IsFuelRequest(string code) =>
        code.StartsWith("FR", StringComparison.OrdinalIgnoreCase);

    private async Task<Unit> DeclineFuelRequestAsync(
        DeclineSupplyRequestCommand request, AccessValidationResult<Unit> access, CancellationToken ct)
    {
        var fuelRequest = await _unitOfWork.FuelRequests.Entities
            .FirstOrDefaultAsync(fr => fr.Id == request.SupplyRequestId && fr.DeletedAt == null, ct);

        if (fuelRequest is null)
            return _errorManager.ThrowNotFound<Unit>("La solicitud no fue encontrada.", "ERP:REQUEST_NOT_FOUND");

        if (!string.Equals(fuelRequest.Code, request.Code, StringComparison.OrdinalIgnoreCase))
            return _errorManager.ThrowBadRequest<Unit>(
                "El código no corresponde a la solicitud.", "ERP:REQUEST_CODE_MISMATCH");

        if (fuelRequest.OutflowStatus != OutflowStatus.PendingReview)
            return _errorManager.ThrowBadRequest<Unit>(
                "La solicitud no está en pendiente de revisión.", "ERP:REQUEST_NOT_PENDING_REVIEW");

        logger.LogInformation("📝 Rechazando solicitud de combustible.");

        // Revertir la reserva de stock (galones) y actualizar estado
        var revertedByStock = new Dictionary<Guid, decimal>
        {
            [fuelRequest.ProductStockId] = fuelRequest.RequestedQuantity
        };
        await StockReservationHelper.RevertStockAsync(_unitOfWork, revertedByStock, ct);

        try
        {
            fuelRequest.OutflowStatus = OutflowStatus.Annuled;
            fuelRequest.AnnulledByUserChiefId = access.Profile.UserId;
            await _unitOfWork.FuelRequests.UpdateAsync(fuelRequest);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error de persistencia al rechazar la solicitud de combustible.");
            return _errorManager.ThrowBadRequest<Unit>(
                "No se pudo rechazar la solicitud de combustible.", "ERP:FUEL_REQUEST_DECLINE_PERSISTENCE_ERROR");
        }

        return Unit.Value;
    }

    private async Task<Unit> DeclineSupplyRequestAsync(
        DeclineSupplyRequestCommand request, AccessValidationResult<Unit> access, CancellationToken ct)
    {
        var supplyRequest = await _unitOfWork.SupplyRequests.Entities
            .Include(sr => sr.SupplyRequestItems)
            .FirstOrDefaultAsync(sr => sr.Id == request.SupplyRequestId && sr.DeletedAt == null, ct);

        if (supplyRequest is null)
            return _errorManager.ThrowNotFound<Unit>("La solicitud no fue encontrada.", "ERP:REQUEST_NOT_FOUND");

        if (!string.Equals(supplyRequest.Code, request.Code, StringComparison.OrdinalIgnoreCase))
            return _errorManager.ThrowBadRequest<Unit>(
                "El código no corresponde a la solicitud.", "ERP:REQUEST_CODE_MISMATCH");

        if (supplyRequest.OutflowStatus != OutflowStatus.PendingReview)
            return _errorManager.ThrowBadRequest<Unit>(
                "La solicitud no está en pendiente de revisión.", "ERP:REQUEST_NOT_PENDING_REVIEW");

        logger.LogInformation("📝 Rechazando solicitud de insumos.");

        // Revertir la reserva de stock (por cada ítem) y actualizar estado
        var revertedByStock = supplyRequest.SupplyRequestItems
            .GroupBy(i => i.ProductStockId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

        await StockReservationHelper.RevertStockAsync(_unitOfWork, revertedByStock, ct);

        try
        {
            supplyRequest.OutflowStatus = OutflowStatus.Annuled;
            supplyRequest.AnnulledByUserChiefId = access.Profile.UserId;
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