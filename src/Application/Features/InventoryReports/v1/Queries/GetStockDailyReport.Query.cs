using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

public class GetStockDailyReportQuery : BaseRequest, IRequest<StockDailyReportDto>
{
    public Guid? BranchId { get; set; }
    public Guid? AreaId { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? CategoryId { get; set; }
    public ProductUsageType? UsageType { get; set; }

    public List<Guid>? BranchIds { get; set; }
    public List<Guid>? AreaIds { get; set; }
    public List<Guid>? CostCenterIds { get; set; }
    public List<Guid>? CategoryIds { get; set; }
    public List<ProductUsageType>? UsageTypes { get; set; }

    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
}