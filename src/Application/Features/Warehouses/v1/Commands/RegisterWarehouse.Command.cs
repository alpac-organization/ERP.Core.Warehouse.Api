using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterWarehouseCommand : BaseRequest, IRequest<bool>
{
    public string Code { get; set; } = null!;
    public Guid BranchId { get; set; }
    public bool IsOwner { get; set; } = true;
    public string WarehouseName { get; set; } = null!;
    public WarehouseType WarehouseType { get; set; }
    public Guid? ParentWarehouseId { get; set; }

    // public List<SectionInformation> AssignedZones { get; set; } = [];
    public required WarehouseDetailsInformation WarehouseDetails { get; set; }
}

public class WarehouseDetailsInformation
{
    public decimal WidthMetres { get; set; }
    public decimal LengthMetres { get; set; }
    public int? RampsCount { get; set; }
    public int? ParkingSpacesCount { get; set; }
}

public class SectionInformation
{
    // public string ZoneName { get; set; } = null!;
    // public decimal WidthMetres { get; set; }
    // public decimal LengthMetres { get; set; }
    // public decimal HeightMetres { get; set; }
    // public decimal MaxWeightCapacityKg { get; set; }
    // public decimal TotalVolumeCapacityM3 { get; set; }

    // public List<RackInformation> AssignedRacks { get; set; } = [];
}

public class RackInformation
{
    // public int RowNumber { get; set; }
    // public int LevelNumber { get; set; }
    // public decimal CostPerPosition { get; set; }
    // public decimal MaxWeightKg { get; set; }
    // public decimal MaxHeightMetres { get; set; }
}
