using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Handlers;

public class GetWarehouseTasksHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper)
    : BaseValidatorHandler<GetWarehouseTasksQuery, List<WarehouseTaskDto>>(unitOfWork, errorManager)
{
    public override async Task<List<WarehouseTaskDto>> Handle(
        GetWarehouseTasksQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId,
            request.CompanyId,
            request.ModuleCode,
            cancellationToken);

        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var query = _unitOfWork.WarehouseTasks.Entities
            .AsNoTracking()
            .Where(task =>
                task.DeletedAt == null &&
                task.Warehouse.Branch.CompanyId == request.CompanyId);

        if (request.WarehouseId.HasValue)
            query = query.Where(task => task.WarehouseId == request.WarehouseId.Value);

        if (request.Status.HasValue)
            query = query.Where(task => task.Status == request.Status.Value);

        if (request.TaskType.HasValue)
            query = query.Where(task => task.TaskType == request.TaskType.Value);

        var tasks = await query
            .OrderByDescending(task => task.CreatedAt)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<WarehouseTaskDto>>(tasks);
    }
}
