using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

public class ResolveMemoryItemsCommand : BaseRequest, IRequest<List<ReassignmentMemoryItemDto>>
{
    public List<Guid> MemoryItemIds { get; set; } = [];

    [JsonIgnore]
    public Guid SessionId { get; set; }
}
