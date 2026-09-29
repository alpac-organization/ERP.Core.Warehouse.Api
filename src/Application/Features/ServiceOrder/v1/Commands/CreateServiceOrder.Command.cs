using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands
{
    public class CreateServiceOrderCommand : BaseRequest, IRequest<Unit>
    {
        public string? Concept { get; set; } 
        public Guid OperationalOrderId { get; set; }
        public Guid OperationalServiceId { get; set; }
    }
}