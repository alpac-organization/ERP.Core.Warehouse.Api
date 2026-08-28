using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

public class TransferSessionCommand : BaseRequest, IRequest<bool>
{
    [JsonIgnore]
    public Guid SessionId { get; set; }

    public string NewOwnerUserId { get; set; } = string.Empty;
}