using System.Text.Json.Serialization;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

public class PauseSessionCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid SessionId { get; set; }
}