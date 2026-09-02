using System;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Dtos
{
    public record CreateWarehouseMachineryDto(
        Guid BranchId,
        Guid WarehouseId,
        Guid? AssignedOperatorId,
        string Code,
        string SerialNumber,
        string? LicensePlate,
        string Name,
        string Brand,
        string Model,
        int ManufactureYear,
        MachineryType MachineryType,
        FuelType FuelType,
        decimal LoadCapacityKg,
        decimal? MaxReachHeightMeters,
        decimal HourMeter,
        MachineryStatus Status,
        string? Notes,
        DateTime? PurchaseDate
    );
    public record WarehouseMachineryListDto(
        Guid Id,
        string Code,
        string SerialNumber,
        string? LicensePlate,
        string Name,
        string Brand,
        string Model,
        string MachineryType,
        string FuelType,
        string Status
    );
}
