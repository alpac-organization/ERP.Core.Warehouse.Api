using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commands
{
    public class CreatePhysicalCountCommand : BaseRequest, IRequest<CreatePhysicalCountResponse>
    {
        public Guid BranchId { get; set; }
        public int TotalLines { get; set; }
        public string? Notes { get; set; }
    }
}
