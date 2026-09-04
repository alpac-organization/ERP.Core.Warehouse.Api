using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Queries;

public class GetWarehouseTasksQuery : BaseRequest, IRequest<List<WarehouseTaskDto>>
{
    public Guid? WarehouseId { get; set; }
    public WarehouseTaskStatus? Status { get; set; }
    public WarehouseTaskType? TaskType { get; set; }
}
