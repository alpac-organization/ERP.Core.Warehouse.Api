using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Queries
{
    public class GetQuoteDetailsQuery : BaseRequest, IRequest<QuotationDto>
    {
        public Guid QuotationId { get; set; }
    }
}