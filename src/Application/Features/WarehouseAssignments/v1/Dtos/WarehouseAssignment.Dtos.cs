using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos;

public class PendingDocumentItemDto
{
    public Guid Id { get; set; }
    public Guid ReceptionId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public string? ServiceOrderCode { get; set; }
    public string? MerchandiseName { get; set; }
    public int? TotalBultos { get; set; }
    public decimal? TotalWeight { get; set; }
    public string PlateNumber { get; set; } = null!;
    public string DriverName { get; set; } = null!;
    public string? ContainerNumber { get; set; }
    public DateOnly? ArrivalDate { get; set; }
    public TimeOnly? ArrivalTime { get; set; }
}

public class WarehouseAssignmentListItemDto
{
    public Guid Id { get; set; }
    public Guid ReceptionId { get; set; }
    public Guid DocumentId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public string PlateNumber { get; set; } = null!;
    public string DriverName { get; set; } = null!;
    public string WarehouseName { get; set; } = null!;
    public WarehouseType WarehouseType { get; set; }
    public string? SectionCode { get; set; }
    public string? RackCode { get; set; }
    public string? LotCode { get; set; }
    public DateTime AssignedAt { get; set; }
    public bool IsCompleted { get; set; }
    public int CrewCount { get; set; }
    public int MachineryCount { get; set; }
}

public class WarehouseAssignmentDetailDto
{
    public Guid Id { get; set; }
    public Guid ReceptionId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public string? ServiceOrderCode { get; set; }
    public string? MerchandiseName { get; set; }
    public int? TotalBultos { get; set; }
    public decimal? TotalWeight { get; set; }
    public string? Remitente { get; set; }
    public string PlateNumber { get; set; } = null!;
    public string DriverName { get; set; } = null!;
    public string? ContainerNumber { get; set; }
    public DateOnly? ArrivalDate { get; set; }
    public TimeOnly? ArrivalTime { get; set; }
    public WarehouseAssignmentDto? Assignment { get; set; }
    public UnloadingDetailsDto? UnloadingDetails { get; set; }
    public UnloadingCrewDto? Crew { get; set; }
    public List<UnloadingMachineryDto> Machinery { get; set; } = [];
}

public class WarehouseAssignmentDto
{
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public string WarehouseCode { get; set; } = null!;
    public WarehouseType WarehouseType { get; set; }
    public Guid? SectionId { get; set; }
    public string? SectionCode { get; set; }
    public Guid RackId { get; set; }
    public string? RackCode { get; set; }
    public Guid? LotsId { get; set; }
    public Guid? LotsPositionsId { get; set; }
    public Guid? RackPositionsId { get; set; }
    public DateTime AssignedAt { get; set; }
    public string AssignedByUserId { get; set; } = null!;
}

public class UnloadingDetailsDto
{
    public Guid UnloadingDetailsId { get; set; }
    public DateTime? UnloadingStartTime { get; set; }
    public DateTime? UnloadingEndTime { get; set; }
    public string? WarehouseChiefUserId { get; set; }
    public decimal? PreparedPallets { get; set; }
}

public class UnloadingCrewDto
{
    public Guid UnloadingCrewAssignmentId { get; set; }
    public int PersonaCount { get; set; }
    public bool Tecerizada { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class UnloadingMachineryDto
{
    public Guid Id { get; set; }
    public Guid MachineryId { get; set; }
    public string MachineryName { get; set; } = null!;
    public string MachineryCode { get; set; } = null!;
    public MachineryType MachineryType { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class AvailableWarehouseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public WarehouseType WarehouseType { get; set; }
    public List<AvailableSectionDto> Sections { get; set; } = [];
}

public class AvailableSectionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public SectionStorageType StorageType { get; set; }
    public List<AvailableRackDto> Racks { get; set; } = [];
    public List<AvailableLotDto> Lots { get; set; } = [];
}

public class AvailableRackDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public RackStatus Status { get; set; }
    public List<AvailablePositionDto>? Positions { get; set; }
}

public class AvailableLotDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public List<AvailablePositionDto>? Positions { get; set; }
}

public class AvailablePositionDto
{
    public Guid Id { get; set; }
    public string PositionCode { get; set; } = null!;
}

public class WarehouseMachineryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public MachineryType MachineryType { get; set; }
    public bool IsActive { get; set; }
}

public class WarehouseStaffDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? Role { get; set; }
    public bool IsActive { get; set; }
}

public class PagedWarehouseAssignmentsDto<T>
{
    public List<T> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
