using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Queries
{
    public class GetOperationalOrdersQuery : BaseRequest, IRequest<PagedResponse<OperationalOrderDto>>
    {
        public string? PoCode { get; set; }
        public string? CustomerCif { get; set; }
        
        public DocumentType? DocumentType { get; set; }
        public OperationalOrderStatus? Status { get; set; }
        
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}