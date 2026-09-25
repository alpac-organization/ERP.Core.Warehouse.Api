using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Commands;

public class PauseWarehouseTaskCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid WarehouseTaskId { get; set; }
}
