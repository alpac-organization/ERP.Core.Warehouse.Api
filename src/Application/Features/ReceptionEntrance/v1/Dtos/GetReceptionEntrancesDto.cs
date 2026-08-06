using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public enum VehicleStatus
{
    OnSite = 1,
    Exited = 2
}
public class ReceptionEntranceListItemDto
{
    public Guid Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public TimeOnly ArrivalTime { get; set; }
    public RecordEntranceStatus Status { get; set; }
    public VehicleStatus VehicleStatus { get; set; }
}

public class GetReceptionEntrancesDto
{
    public List<ReceptionEntranceListItemDto> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    public ReceptionEntranceStatsDto Stats { get; set; } = new();
}

public class EntranceDucatDetailItemDto
{
    public Guid Id { get; set; }
    public string DucatNumber { get; set; } = string.Empty;
}

public class CustomsDeclarationDetailDto
{
    public string CustomsDecarationNumber { get; set; } = string.Empty;
    public int? Packages { get; set; }
    public string? Customer { get; set; }
    public string? Product { get; set; }
    public string? ContainerNumber { get; set; }
}

public class ExecutionLogDetailDto
{
    public DateOnly StartDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string ProcessedByUserName { get; set; } = string.Empty;
    public int? DurationTotalSeconds { get; set; }
    public string? DurationFormatted { get; set; }
}

public class ReceptionEntranceDetailDto
{
    public Guid Id { get; set; }
    public RecordEntranceStatus Status { get; set; }
    public bool IsConsolidated { get; set; }

    public string CountryOfOrigin { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string TrailerChassis { get; set; } = string.Empty;
    public string DriverLicense { get; set; } = string.Empty;
    public string Transportista { get; set; } = string.Empty;
    public Guid TransportUnitId { get; set; }
    public string? TransportUnitName { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string SealNumber { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public DateOnly? TransportUnitExitDate { get; set; }
    public TimeOnly? TransportUnitExitTime { get; set; }
    public string? UpdatedByUserName { get; set; }
    public DateOnly? UpdatedDate { get; set; }
    public TimeOnly? UpdatedTime { get; set; }

    public List<EntranceDucatDetailItemDto>? Ducats { get; set; }
    public CustomsDeclarationDetailDto? CustomsDeclaration { get; set; }
    public ExecutionLogDetailDto? ExecutionLog { get; set; }
}

public class ReceptionEntranceStatsDto
{
    public int TotalEntries { get; set; }
    public int TotalOnSite { get; set; }
    public int TotalExists { get; set; }
}

public class TransportUnitListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}