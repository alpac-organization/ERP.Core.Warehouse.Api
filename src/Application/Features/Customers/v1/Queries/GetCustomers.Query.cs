using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Queries
{
    public class GetCustomersQuery : BaseRequest, IRequest<PagedResponse<CustomerDto>>
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}