using MediatR;
using ERP.Core.Domain.Entities.Bases;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries
{
    public class GetPhysicalCountPdfQuery : BaseRequest, IRequest<PhysicalCountPdfResult>
    {
        public Guid PhysicalCountId { get; set; }
    }
}
