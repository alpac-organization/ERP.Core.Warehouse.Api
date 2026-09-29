using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

public class WarehouseDetailDto
{
    public Guid WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public WarehouseType WarehouseType { get; set; }
    public WarehouseLocationDto? Location { get; set; }
    public WarehouseCapacitiesDto? Capacity { get; set; }
    public List<SectionSummaryDto> Sections { get; set; } = [];
}

public class WarehouseLocationDto
{
    public string? LocationName { get; set; }
}

public class SectionSummaryDto
{
    public Guid SectionId { get; set; }
    public string? Code { get; set; }
    public bool IsActive { get; set; }
    public SectionType SectionType { get; set; }
    public SectionStorageType SectionStorageType { get; set; }
}
