using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;

public class GetSupplyRequestDto
{
    public Guid SupplyRequestId { get; set; }
    public string SupplyRequestCode { get; set; } = null!;
    public ReasonUse ReasonUse { get; set; }
    public string? Justification { get; set; }
    public OutflowStatus OutflowStatus { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string RequestedByUserName { get; set; } = null!;

    public int ItemsCount { get; set; }
}