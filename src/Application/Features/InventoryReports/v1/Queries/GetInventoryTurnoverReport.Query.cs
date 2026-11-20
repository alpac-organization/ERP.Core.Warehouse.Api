using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

public class GetInventoryTurnoverReportQuery : BaseRequest, IRequest<List<InventoryTurnoverReportDto>>
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductCode { get; set; }
}