using MediatR;
using ERP.Core.Domain.Entities.Bases;
using System.Text.Json.Serialization;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Commands
{
    public class ConfirmInventoryOutputCommand : BaseRequest, IRequest<Unit>
    {
        [JsonIgnore]
        public Guid InventoryOutputId { get; set; }
    }
}