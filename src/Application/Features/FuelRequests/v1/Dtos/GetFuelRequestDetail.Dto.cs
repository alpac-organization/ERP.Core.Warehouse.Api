using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Dtos;

public class GetFuelRequestDetailDto : GetFuelRequestDto
{
    public WorkAreaInformation WorkAreaInformation { get; set; } = default!;
    public CostCenterInformation CostCenterInformation { get; set; } = default!;
    public BranchInformation BranchInformation { get; set; } = default!;
    // public MachineryInformation MachineryInformation { get; set; } = default!;
    public string? ApprovedByUserChiefName { get; set; }
    public string? AnnulledByUserChiefName { get; set; }
    public Guid ProductStockId { get; set; }
    public string ProductStockName { get; set; } = null!;
    public decimal RequestedQuantity { get; set; }
    // public decimal? Mileage { get; set; }
    // public decimal? EngineHours { get; set; }

}