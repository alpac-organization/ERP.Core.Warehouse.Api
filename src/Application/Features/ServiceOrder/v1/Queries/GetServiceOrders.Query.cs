using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Queries
{
    public class GetServiceOrdersQuery : BaseRequest, IRequest<PagedResponse<ServiceOrderDto>>
    {   
        
        public string? Code { get; set; }

        public string? CustomerCif { get; set; }


        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
