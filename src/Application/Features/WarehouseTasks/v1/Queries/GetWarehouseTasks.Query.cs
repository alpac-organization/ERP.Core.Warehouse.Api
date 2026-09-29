using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Queries;

public class GetWarehouseTasksQuery : BaseRequest, IRequest<List<WarehouseTaskDto>>
{
    public Guid? WarehouseId { get; set; }
    public WarehouseTaskStatus? Status { get; set; }
    public WarehouseTaskType? TaskType { get; set; }
}
