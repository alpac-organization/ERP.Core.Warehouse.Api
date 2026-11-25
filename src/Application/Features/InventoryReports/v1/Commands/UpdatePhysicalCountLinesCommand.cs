using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commands
{
    public class UpdatePhysicalCountLinesCommand : BaseRequest, IRequest<Unit>
    {
        public Guid PhysicalCountId { get; set; }
        public List<UpdatePhysicalCountLineItem> Items { get; set; } = [];
    }

    public class UpdatePhysicalCountLineItem
    {
        public Guid PhysicalCountLineId { get; set; }
        public decimal? PhysicalStock { get; set; }
        public string? Observations { get; set; }
    }
}
