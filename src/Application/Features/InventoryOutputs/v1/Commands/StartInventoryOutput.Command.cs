using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Commands
{
    public class StartInventoryOutputCommand : BaseRequest, IRequest<Unit>
    {
        [JsonIgnore]
        public Guid InventoryOutputId { get; set; }

        // [JsonPropertyName("is_all_dispached")]
        public bool IsAllDispached { get; set; } = true;

        public string? DispatchedJustification { get; set; }

        public List<StartInventoryOutputItem> Items { get; set; } = [];
    }

    public class StartInventoryOutputItem
    {
        public Guid ProductStockId { get; set; }
        public decimal DispatchedQuantity { get; set; }
    }
}