
namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

public class WarehouseCapacitiesDto
{
    public decimal Width { get; set; }
    public decimal Length { get; set; }
    public bool HasMargins { get; set; }
    public decimal? MinimumHeight { get; set; }
    public decimal? MaximumHeight { get; set; }
    public decimal? MarginTop { get; set; }
    public decimal? MarginBottom { get; set; }
    public decimal? MarginLeft { get; set; }
    public decimal? MarginRight { get; set; }

    // M2
    public decimal TotalAreaM2 { get; set; }
    public decimal UnusedAreaM2 { get; set; }
    public decimal AvailableAreaWithMarginM2 { get; set; }
    public decimal OccupiedChargeableAreaM2 { get; set; }
    public decimal UnoccupiedChargeableAreaM2 { get; set; }
    public decimal PercentageAvailableAreaWithMarginM2 { get; set; }

    // M3
    public decimal? TotalVolumenM3 { get; set; }
    public decimal? UnusedVolumenM3 { get; set; }
    public decimal? AvailableVolumenWithMarginM3 { get; set; }
    public decimal? OccupiedChargeableVolumenM3 { get; set; }
    public decimal? UnoccupiedChargeableVolumenM3 { get; set; }
    public decimal PercentageAvailableVolumenWithMarginM3 { get; set; }
}