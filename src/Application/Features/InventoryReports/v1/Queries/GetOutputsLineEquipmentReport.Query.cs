using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

public class GetOutputsLineEquipmentReportQuery : BaseRequest, IRequest<OutputLineEquipmentReportDto>
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? BranchId { get; set; }
    public List<Guid>? CategoryId { get; set; }
    public List<Guid>? EquipmentIds { get; set; }
    public List<Guid>? ProductIds { get; set; }
}