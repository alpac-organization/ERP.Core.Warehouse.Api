using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;

public class GetSupplyRequestDto
{
    public Guid SupplyRequestId { get; set; }
    public string Code { get; set; } = null!;
    public string? Justification { get; set; }
    public OutflowStatus OutflowStatus { get; set; }
    public Guid WorkAreaId { get; set; }
    public Guid CostCenterId { get; set; }
    public Guid BranchId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid OperationalOrderId { get; set; }
    public Guid RequestedByUserId { get; set; }
}