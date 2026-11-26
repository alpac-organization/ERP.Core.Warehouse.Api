using MediatR;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

public class GetInventoryMovementReportPdfQuery : InventoryReportQueryBase, IRequest<ReportPdfFileDto>
{
    public Guid? CostCenterId { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductCode { get; set; }
}
