using System;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Dtos
{
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
