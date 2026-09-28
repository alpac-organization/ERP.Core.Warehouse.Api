using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Queries
{
    public class GetOperationalOrderDetailsQuery : BaseRequest, IRequest<OperationalOrderDetailsDto>
    {
        public Guid OperationalOrderId { get; set; }
    }
}