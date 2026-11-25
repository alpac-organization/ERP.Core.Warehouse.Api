namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

public abstract class OutputReportQueryBase : InventoryReportQueryBase
{
    public List<Guid>? CategoryId { get; set; }
    public List<Guid>? EquipmentIds { get; set; }
    public List<Guid>? ProductIds { get; set; }
}
