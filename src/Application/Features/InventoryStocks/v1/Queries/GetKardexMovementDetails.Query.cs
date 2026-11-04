using MediatR;
using System.Text.Json.Serialization;

using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Queries
{
    public class GetKardexMovementDetailsQuery : BaseRequest, IRequest<KardexDetailsDto>
    {
        [JsonIgnore]
        public Guid KardexId { get; set; }
    }
}