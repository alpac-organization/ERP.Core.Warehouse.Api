using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Commands;

public class ReserveUnloadingPositionsCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid AssignmentId { get; set; }

    public List<PositionReservationItemDto> Positions { get; set; } = [];
}

public class PositionReservationItemDto
{
    public Guid? RackPositionId { get; set; }
    public Guid? LotPositionId { get; set; }
}