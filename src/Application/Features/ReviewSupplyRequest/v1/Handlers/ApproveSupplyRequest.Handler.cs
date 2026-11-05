using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Helpers.Inventory;
using ERP.Core.Warehouse.Api.Application.Features.ReviewSupplyRequest.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReviewSupplyRequest.v1.Handlers;

public class ApproveSupplyRequestHandler(
    IUnitOfWork _unitOfWork, IErrorManager _errorManager, ICodeGenerator _codeGenerator, ILogger<ApproveSupplyRequestHandler> logger)
        : BaseValidatorHandler<ApproveSupplyRequestCommand, Unit>(_unitOfWork, _errorManager)
{
    public override async Task<Unit> Handle(ApproveSupplyRequestCommand request, CancellationToken ct)
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

        var reasonExists = await _unitOfWork.ReasonsInventoryRemoval.Entities
            .AsNoTracking()
            .AnyAsync(r => r.Id == request.ReasonInventoryRemovalId && r.IsActive && r.DeletedAt == null, ct);

        if (!reasonExists)
            return _errorManager.ThrowBadRequest<Unit>(
                "El motivo de salida de inventario no es válido.", "ERP:REASON_INVENTORY_REMOVAL_NOT_FOUND");

        if (IsFuelRequest(request.Code))
            return await ApproveFuelRequestAsync(request, access, ct);

        return await ApproveSupplyRequestAsync(request, access, ct);
    }

    private static bool IsFuelRequest(string code) =>
        code.StartsWith("FR", StringComparison.OrdinalIgnoreCase);

    private async Task<Unit> ApproveFuelRequestAsync(
        ApproveSupplyRequestCommand request, AccessValidationResult<Unit> access, CancellationToken ct)
    {
        var fuelRequest = await _unitOfWork.FuelRequests.Entities
            .FirstOrDefaultAsync(fr => fr.Id == request.SupplyRequestId && fr.DeletedAt == null, ct);

        if (fuelRequest is null)
            return _errorManager.ThrowNotFound<Unit>(
                "La solicitud de combustible no fue encontrada.", "ERP:FUEL_REQUEST_NOT_FOUND");

        if (!string.Equals(fuelRequest.Code, request.Code, StringComparison.OrdinalIgnoreCase))
            return _errorManager.ThrowBadRequest<Unit>(
                "El código no corresponde a la solicitud.", "ERP:REQUEST_CODE_MISMATCH");

        if (fuelRequest.OutflowStatus != OutflowStatus.PendingReview)
            return _errorManager.ThrowBadRequest<Unit>(
                "La solicitud de combustible no está en pendiente de revisión.", "ERP:FUEL_REQUEST_NOT_PENDING_REVIEW");

        logger.LogInformation("📝 Aprobando solicitud de combustible.");

        var (branchCode, branchCodeError) = await SupplyRequestSharedHelper.GetBranchCodeAsync(
            _unitOfWork, _errorManager, fuelRequest.BranchId, ct);

        if (branchCodeError is not null)
            return branchCodeError.Value;

        var (isSuccess, generatedCode) =
            await _codeGenerator.GenerateCodeToInventoryPendingOutPuts(branchCode!, ct);

        if (!isSuccess || string.IsNullOrWhiteSpace(generatedCode))
            return _errorManager.ThrowBadRequest<Unit>(
                "No se pudo generar el código para la salida de inventario.", "ERP:INVENTORY_OUTPUT_CODE_GENERATION_ERROR");

        try
        {
            fuelRequest.OutflowStatus = OutflowStatus.Approved;
            fuelRequest.ApprovedByUserChiefId = access.Profile.UserId;
            await _unitOfWork.FuelRequests.UpdateAsync(fuelRequest);

            var inventoryOutput = request.ToInventoryOutputEntity(fuelRequest, generatedCode);

            await _unitOfWork.InventoryOutputs.RegisterInventoryOutput(inventoryOutput);

            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error de persistencia al aprobar la solicitud de combustible.");
            return _errorManager.ThrowBadRequest<Unit>(
                "No se pudo aprobar la solicitud de combustible.", "ERP:FUEL_REQUEST_APPROVAL_PERSISTENCE_ERROR");
        }

        return Unit.Value;
    }

    private async Task<Unit> ApproveSupplyRequestAsync(
        ApproveSupplyRequestCommand request, AccessValidationResult<Unit> access, CancellationToken ct)
    {
        var supplyRequest = await _unitOfWork.SupplyRequests.Entities
            .FirstOrDefaultAsync(sr => sr.Id == request.SupplyRequestId && sr.DeletedAt == null, ct);

        if (supplyRequest is null)
            return _errorManager.ThrowNotFound<Unit>(
                "La solicitud de insumos no fue encontrada.", "ERP:SUPPLY_REQUEST_NOT_FOUND");

        if (!string.Equals(supplyRequest.Code, request.Code, StringComparison.OrdinalIgnoreCase))
            return _errorManager.ThrowBadRequest<Unit>(
                "El código no corresponde a la solicitud.", "ERP:REQUEST_CODE_MISMATCH");

        if (supplyRequest.OutflowStatus != OutflowStatus.PendingReview)
            return _errorManager.ThrowBadRequest<Unit>(
                "La solicitud de insumos no está en pendiente de revisión.", "ERP:SUPPLY_REQUEST_NOT_PENDING_REVIEW");

        logger.LogInformation("📝 Aprobando solicitud de insumos.");

        var branchCode = await _unitOfWork.Branches.Entities
            .AsNoTracking()
            .Where(b => b.Id == supplyRequest.BranchId && b.IsActive && b.DeletedAt == null)
            .Select(b => b.BranchCode)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(branchCode))
            return _errorManager.ThrowBadRequest<Unit>(
                "No se encontró el código de sucursal.", "ERP:BRANCH_CODE_NOT_FOUND");

        var (isSuccess, generatedCode) =
            await _codeGenerator.GenerateCodeToInventoryPendingOutPuts(branchCode, ct);

        if (!isSuccess || string.IsNullOrWhiteSpace(generatedCode))
            return _errorManager.ThrowBadRequest<Unit>(
                "No se pudo generar el código para la salida de inventario.", "ERP:INVENTORY_OUTPUT_CODE_GENERATION_ERROR");

        try
        {
            supplyRequest.OutflowStatus = OutflowStatus.Approved;
            supplyRequest.ApprovedByUserChiefId = access.Profile.UserId;
            await _unitOfWork.SupplyRequests.UpdateAsync(supplyRequest);

            var inventoryOutput = request.ToInventoryOutputEntity(supplyRequest, generatedCode);

            await _unitOfWork.InventoryOutputs.RegisterInventoryOutput(inventoryOutput);

            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error de persistencia al aprobar la solicitud de insumos.");
            return _errorManager.ThrowBadRequest<Unit>(
                "No se pudo aprobar la solicitud de insumos.", "ERP:SUPPLY_REQUEST_APPROVAL_PERSISTENCE_ERROR");
        }

        return Unit.Value;
    }
}