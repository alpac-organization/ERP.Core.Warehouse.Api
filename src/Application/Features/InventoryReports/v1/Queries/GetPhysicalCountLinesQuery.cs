using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries
{
    public class GetPhysicalCountLinesQuery : BaseRequest, IRequest<PhysicalCountLinesPageDto>
    {
        public Guid PhysicalCountId { get; set; }

        public int PageSize { get; set; } = 30;
        public int PageNumber { get; set; } = 1;
    }
}
