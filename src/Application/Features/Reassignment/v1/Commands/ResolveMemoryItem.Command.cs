using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

public class ResolveMemoryItemCommand : BaseRequest, IRequest<ReassignmentMemoryItemDto>
{
    [JsonIgnore]
    public Guid SessionId { get; set; }

    [JsonIgnore]
    public Guid MemoryItemId { get; set; }
}
