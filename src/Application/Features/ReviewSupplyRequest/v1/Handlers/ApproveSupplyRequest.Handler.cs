using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.ReviewSupplyRequest.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReviewSupplyRequest.v1.Handlers;

/// <summary>
/// Handler para aprovacion de solicitud por parte del superior del solicitante, paso antes de llegar al responsable de bodega
/// </summary>

public class ApproveSupplyRequestHandler(
    IUnitOfWork _unitOfWork, IErrorManager _errorManager, ICodeGenerator _codeGenerator, ILogger<ApproveSupplyRequestHandler> logger)
        : BaseValidatorHandler<ApproveSupplyRequestCommand, Unit>(_unitOfWork, _errorManager)
{
    public override async Task<Unit> Handle(ApproveSupplyRequestCommand request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var supplyRequest = await _unitOfWork.SupplyRequests.Entities
            .FirstOrDefaultAsync(sr => sr.Id == request.SupplyRequestId && sr.DeletedAt == null, ct);

        if (supplyRequest is null)
            return _errorManager.ThrowNotFound<Unit>(
                "La solicitud de insumos no fue encontrada.", "ERP:SUPPLY_REQUEST_NOT_FOUND");

        if (supplyRequest.OutflowStatus != OutflowStatus.PendingReview)
            return _errorManager.ThrowBadRequest<Unit>(
                "La solicitud de insumos no está en pendiente de revisión.", "ERP:SUPPLY_REQUEST_NOT_PENDING_REVIEW");

        var allowedArea = await _unitOfWork.InventoryAreasPermissions.Entities
            .AsNoTracking()
            .Where(a => a.WorkAreaId == access.Profile.AreaId && a.IsActive)
            .FirstOrDefaultAsync(ct);

        if (allowedArea is null || access.Role?.RoleType is not (RoleType.Supervisor or RoleType.Administrator))
            return _errorManager.ThrowBadRequest<Unit>(
                "No tienes permiso para esta acción.", "ERP:USER_NOT_AUTOIZED");

        var reasonExists = await _unitOfWork.ReasonsInventoryRemoval.Entities
            .AsNoTracking()
            .AnyAsync(r => r.Id == request.ReasonInventoryRemovalId && r.IsActive && r.DeletedAt == null, ct);

        if (!reasonExists)
            return _errorManager.ThrowBadRequest<Unit>(
                "El motivo de salida de inventario no es válido.", "ERP:REASON_INVENTORY_REMOVAL_NOT_FOUND");

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
