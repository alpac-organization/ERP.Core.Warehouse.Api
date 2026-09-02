using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Handlers;

public class StartUnloadingHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<StartUnloadingCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(StartUnloadingCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        #region 1. Validar paso DESC configurado
        var descConfigured = await _unitOfWork.WorkflowStepDefinitions.Entities
            .AnyAsync(x => x.Code == WorkflowStepCodes.Unloading, cancellationToken);

        if (!descConfigured)
        {
            return _errorManager.ThrowBadRequest<bool>(
                $"No se encontró la configuración del paso '{WorkflowStepCodes.Unloading}' en WorkflowStepDefinitions. Contacte al administrador.",
                "ERP:WORKFLOW_NOT_CONFIGURED");
        }
        #endregion

        #region 2. Obtener y validar asignación
        var assignment = await _unitOfWork.WarehouseAssignments.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId && a.DeletedAt == null, cancellationToken);

        if (assignment is null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "La asignación no fue encontrada o ya ha sido eliminada.",
                "ERP:ASSIGNMENT_NOT_FOUND");
        }

        if (assignment.UnloadingStatus != UnloadingStatus.Pending)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "La asignación no está en estado Pendiente; no se puede iniciar la descarga.",
                "ERP:ASSIGNMENT_NOT_PENDING");
        }
        #endregion

        var now = NicaraguaClock.Now;
        var startDate = request.StartDate ?? NicaraguaClock.Today;
        var startTime = request.StartTime ?? NicaraguaClock.TimeNow;

        #region 3. Insertar UnloadingDetails
        var detail = new UnloadingDetails
        {
            WarehouseAssignmentId = request.AssignmentId,
            MerchandiseType = request.MerchandiseType
        };

        await _unitOfWork.UnloadingDetails.InsertUnloadingDetail(detail);
        #endregion

        #region 4. Insertar UnloadingPallets
        foreach (var pallet in request.Pallets)
        {
            var palletEntity = new UnloadingPallets
            {
                UnloadingDetailsId = detail.Id,
                PalletType = pallet.Type,
                Quantity = pallet.Quantity,
                LengthMetres = pallet.Type == PalletType.Standard ? (decimal?)null : pallet.LenghtMetres,
                WidthMetres = pallet.Type == PalletType.Standard ? (decimal?)null : pallet.WidthMetres
            };

            await _unitOfWork.UnloadingPallets.InsertUnloadingPallet(palletEntity);
        }
        #endregion

        #region 5. Insertar UnloadingSupplies
        foreach (var supply in request.Supplies)
        {
            var supplyEntity = new UnloadingSupplies
            {
                UnloadingDetailsId = detail.Id,
                Description = supply.Description,
                Quantity = supply.Quantity
            };

            await _unitOfWork.UnloadingSupplies.InsertUnloadingSupplie(supplyEntity);
        }
        #endregion

        #region 6. Actualizar estado de la asignación
        var assignmentUpdate = await _unitOfWork.WarehouseAssignments.Entities
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);

        if (assignmentUpdate is not null)
        {
            assignmentUpdate.UnloadingStatus = UnloadingStatus.InProgress;
            assignmentUpdate.UnloadingStartTime = now;
        }
        #endregion

        #region 7. Insertar StepExecutionLog (DESC)
        var executionLog = new StepExecutionLogs
        {
            RecordEntranceId = assignment.RecordEntranceId,
            WorkflowStepDefinitionCode = WorkflowStepCodes.Unloading,
            StartDate = startDate,
            StartTime = startTime,
            ProcessedByUserId = request.UserId.ToString(),
            ProcessedByUserName = request.ProcessedByUserName
        };

        await _unitOfWork.StepExecutionLogs.InsertExecutionLog(executionLog);
        #endregion

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
