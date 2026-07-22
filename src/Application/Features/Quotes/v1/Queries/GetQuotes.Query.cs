using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Queries
{
    public class GetQuotesQuery : BaseRequest, IRequest<PagedResponse<QuotationDto>>
    {
        public Guid? BranchId { get; set; }
        public string? QuoteCode { get; set;}

        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }

}