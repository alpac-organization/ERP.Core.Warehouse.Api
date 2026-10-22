using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Queries
{
    public record PagedResponseStock<T>(
        List<T> Data,
        int PageNumber,
        int PageSize,
        int Total
    ) : PagedResponse<T>(Data, PageNumber, PageSize, Total);

    public class GetInventoryStocksQuery : BaseRequest, IRequest<PagedResponseStock<InventoryStockDto>>
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}