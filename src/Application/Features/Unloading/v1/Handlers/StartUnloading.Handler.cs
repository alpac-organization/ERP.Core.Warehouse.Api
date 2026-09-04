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
    private readonly IMapper _mapper = mapper;

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
            .Where(a => a.Id == request.AssignmentId && a.DeletedAt == null)
            .Select(a => new { a.Id, a.WarehouseId, a.RecordEntranceId, a.UnloadingStatus })
            .FirstOrDefaultAsync(cancellationToken);

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

        #region 2.5. Obtener y validar registro de recepción
        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Where(r => r.Id == assignment.RecordEntranceId && r.DeletedAt == null)
            .Select(r => new { r.Id, r.CurrentStepCode, r.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (recordEntrance is null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        if (recordEntrance.Status == RecordEntranceStatus.Completed ||
            recordEntrance.Status == RecordEntranceStatus.Abandoned)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "No se puede iniciar la descarga porque el registro ya está completado o abandonado.",
                "ERP:RECORD_ALREADY_CLOSED");
        }
        #endregion

        var now = NicaraguaClock.Now;

        var user = await _unitOfWork.Users.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        var processedByUserName = user?.Fullname ?? user?.UserName ?? request.UserId.ToString();

        #region 3. Insertar UnloadingDetails
        var detail = _mapper.Map<UnloadingDetails>(request);

        await _unitOfWork.UnloadingDetails.InsertUnloadingDetail(detail);
        #endregion

        #region 4. Insertar UnloadingPallets
        foreach (var pallet in request.Pallets)
        {
            var palletEntity = _mapper.Map<UnloadingPallets>(pallet, opts =>
                opts.Items["UnloadingDetailsId"] = detail.Id);

            await _unitOfWork.UnloadingPallets.InsertUnloadingPallet(palletEntity);
        }
        #endregion

        #region 5. Insertar UnloadingSupplies
        foreach (var supply in request.Supplies)
        {
            var supplyEntity = _mapper.Map<UnloadingSupplies>(supply, opts =>
                opts.Items["UnloadingDetailsId"] = detail.Id);

            await _unitOfWork.UnloadingSupplies.InsertUnloadingSupplie(supplyEntity);
        }
        #endregion

        #region 6. Insertar StepExecutionLog (DESC)
        var executionLog = _mapper.Map<StepExecutionLogs>(request, opts =>
        {
            opts.Items["RecordEntranceId"] = assignment.RecordEntranceId;
            opts.Items["ProcessedByUserName"] = processedByUserName;
        });

        await _unitOfWork.StepExecutionLogs.InsertExecutionLog(executionLog);
        #endregion

        #region 7. Persistir inserts y actualizar estado de la asignación
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var unloadingDetailsId = detail.Id;

        await _unitOfWork.WarehouseAssignments.Entities
            .Where(a => a.Id == request.AssignmentId && a.DeletedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.UnloadingStatus, UnloadingStatus.InProgress)
                .SetProperty(a => a.UnloadingStartTime, now),
                cancellationToken);

        if (recordEntrance.Status != RecordEntranceStatus.Unloading ||
            recordEntrance.CurrentStepCode != WorkflowStepCodes.Unloading)
        {
            await _unitOfWork.RecordEntrance.Entities
                .Where(r => r.Id == assignment.RecordEntranceId && r.DeletedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.CurrentStepCode, WorkflowStepCodes.Unloading)
                    .SetProperty(r => r.Status, RecordEntranceStatus.Unloading),
                    cancellationToken);
        }
        #endregion

        #region 8. Registrar tarea de descarga
        var warehouseTask = _mapper.Map<WarehouseTask>(request, opts =>
        {
            opts.Items["WarehouseId"] = assignment.WarehouseId;
            opts.Items["SourceId"] = unloadingDetailsId;
            opts.Items["StartedAt"] = now;
        });

        await _unitOfWork.WarehouseTasks.InsertWarehouseTask(warehouseTask);

        var warehouseTaskEvent = _mapper.Map<WarehouseTaskEvent>(request, opts =>
        {
            opts.Items["WarehouseTaskId"] = warehouseTask.Id;
            opts.Items["OccurredAt"] = now;
        });
        await _unitOfWork.WarehouseTaskEvents.InsertWarehouseTaskEvent(warehouseTaskEvent);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion

        return true;
    }
}
