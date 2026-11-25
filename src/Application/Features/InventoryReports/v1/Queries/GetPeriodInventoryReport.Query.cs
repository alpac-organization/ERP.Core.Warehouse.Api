using MediatR;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

public class GetPeriodInventoryReportQuery : InventoryReportQueryBase, IRequest<PeriodInventoryReportDto>
{
    public Guid? CostCenterId { get; set; }
}
