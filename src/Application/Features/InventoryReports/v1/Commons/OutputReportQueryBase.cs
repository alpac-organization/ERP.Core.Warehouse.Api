using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

public abstract class OutputReportQueryBase : BaseRequest
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? BranchId { get; set; }
    public List<Guid>? CategoryId { get; set; }
    public List<Guid>? EquipmentIds { get; set; }
    public List<Guid>? ProductIds { get; set; }
}
