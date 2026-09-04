using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Handlers;

public class PauseWarehouseTaskHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<PauseWarehouseTaskCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(PauseWarehouseTaskCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var task = await _unitOfWork.WarehouseTasks.Entities
            .FirstOrDefaultAsync(t => t.Id == request.WarehouseTaskId && t.DeletedAt == null, cancellationToken);

        if (task is null)
            return _errorManager.ThrowNotFound<bool>("La tarea de bodega no existe.", "ERP:WAREHOUSE_TASK_NOT_FOUND");

        if (task.CurrentOwnerUserId != request.UserId.ToString())
            return _errorManager.ThrowForbidden<bool>("Solo el responsable actual puede pausar la tarea.", "ERP:WAREHOUSE_TASK_NOT_OWNER");

        if (task.Status != WarehouseTaskStatus.InProgress)
            return _errorManager.ThrowBadRequest<bool>("La tarea no está en progreso; no se puede pausar.", "ERP:WAREHOUSE_TASK_NOT_IN_PROGRESS");

        var now = NicaraguaClock.Now;
        task.Status = WarehouseTaskStatus.Paused;
        task.PausedAt = now;

        await _unitOfWork.WarehouseTaskEvents.InsertWarehouseTaskEvent(
            mapper.Map<WarehouseTaskEvent>(request, options =>
            {
                options.Items["WarehouseTaskId"] = task.Id;
                options.Items["EventType"] = WarehouseTaskEventType.Paused;
                options.Items["Status"] = WarehouseTaskStatus.Paused;
                options.Items["OccurredAt"] = now;
            }));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
