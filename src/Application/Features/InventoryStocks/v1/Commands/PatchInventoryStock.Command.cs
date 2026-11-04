using MediatR;
using ERP.Core.Domain.Entities.Bases;
using System.Text.Json.Serialization;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Commands
{
    public class PatchInventoryStockCommand : BaseRequest, IRequest<Unit>
    {
        [JsonIgnore]
        public Guid InventoryStockId { get; set; }

        public decimal? MinimumStock { get; set; }
        public decimal? MaximumStock { get; set; }
    }
}