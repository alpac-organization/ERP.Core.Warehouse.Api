using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Commands;

public class ResumeWarehouseTaskCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid WarehouseTaskId { get; set; }
}
