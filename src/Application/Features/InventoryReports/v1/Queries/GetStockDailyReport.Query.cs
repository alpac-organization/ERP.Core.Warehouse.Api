using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

public class GetStockDailyReportQuery : BaseRequest, IRequest<StockDailyReportDto>
{
    public Guid? BranchId { get; set; }
    public Guid? AreaId { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
}