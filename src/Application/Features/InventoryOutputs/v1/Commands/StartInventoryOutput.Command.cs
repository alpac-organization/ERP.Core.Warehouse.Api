using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Commands
{
    public class StartInventoryOutputCommand : BaseRequest, IRequest<Unit>
    {
        [JsonIgnore]
        public Guid InventoryOutputId { get; set; }
    }
}