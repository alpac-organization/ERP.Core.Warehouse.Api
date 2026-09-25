using MediatR;
using System.Text.Json.Serialization;

using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

public class OpenReassignmentSessionCommand : BaseRequest, IRequest<ReassignmentSessionDto>
{
    [JsonIgnore]
    public Guid WarehouseId { get; set; }
}
