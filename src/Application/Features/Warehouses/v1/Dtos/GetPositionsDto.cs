using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

public class GetPositionsDto
{
    public Guid WarehouseId { get; set; }
    public Guid SectionId { get; set; }
    public string SectionCode { get; set; } = string.Empty;
    public SectionType SectionType { get; set; }
    public SectionStorageType SectionStorageType { get; set; }
    public List<PositionBlockDto> Blocks { get; set; } = [];
}

public class PositionBlockDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public List<PositionItemDto> Positions { get; set; } = [];
}

public class PositionItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Level { get; set; }
    public RackStatus Status { get; set; }
    public PositionCoordinatesDto? Coordinates { get; set; }
}

public class PositionCoordinatesDto
{
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public decimal PositionZ { get; set; }
    public decimal RotationY { get; set; }
}
