using MediatR;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Queries
{
    public class GetQuotationsQuery : BaseRequest, IRequest<PagedResponse<QuotationDto>>
    {
        public Guid PurchaseRequestItemId { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
