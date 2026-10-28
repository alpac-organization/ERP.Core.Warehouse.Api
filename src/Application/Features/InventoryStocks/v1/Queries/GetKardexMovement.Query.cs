using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Queries
{
    public class GetKardexMovementQuery : BaseRequest, IRequest<PagedResponse<KardexDto>>
    {
        public Guid? CategoryId { get; set; }
        public string? ProductCode { get; set; }
        public KardexType? KardexType { get; set; }
        public KardexMovementType? KardexMovementType { get; set; }
        
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}   