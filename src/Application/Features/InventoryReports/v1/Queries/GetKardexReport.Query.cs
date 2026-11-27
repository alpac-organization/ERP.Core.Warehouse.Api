using MediatR;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

public class GetKardexReportQuery : InventoryReportQueryBase, IRequest<ReportPdfFileDto>
{
    public Guid? CostCenterId { get; set; }
    public List<Guid>? ProductStockIds { get; set; }
}
