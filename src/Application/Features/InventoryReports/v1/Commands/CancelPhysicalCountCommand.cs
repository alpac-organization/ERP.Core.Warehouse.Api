using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commands
{
    public class CancelPhysicalCountCommand : BaseRequest, IRequest<bool>
    {
        public Guid PhysicalCountId { get; set; }
    }
}
