using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Dtos;

public class GetInventoryOutputDto
{
    public Guid InventoryOutputId { get; set; }
    public string OutputCode { get; set; } = null!;
    public string? Observations { get; set; }
    public InventoryOutputsStatus Status { get; set; }
    public string? RequestCode { get; set; }
    public string? RequestType { get; set; }
    public string? ReasonsInventoryRemovalDescription { get; set; }
    public string? WorkAreaName { get; set; }
    public string? CostCenterName { get; set; }
    public DateTime? RequestDate { get; set; }
}