using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public class ReceptionEntranceStatsDto
{
    public int TotalEntries { get; set; }
    public int TotalOnSite { get; set; }
    public int TotalExits { get; set; }
}

public class EntranceDucatItemDto
{
    public Guid Id { get; set; }
    public string DucatNumber { get; set; } = string.Empty;
}

public class StepExecutionLogItemDto
{
    public DateOnly StartDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string ProcessedByUserName { get; set; } = string.Empty;

    public int? DurationTotalSeconds { get; set; }
    public string? DurationFormatted { get; set; }
}

public class ReceptionEntranceItemDto
{
    public Guid Id { get; set; }
    public string CountryOfOrigin { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string TrailerChassis { get; set; } = string.Empty;
    public string DriverLicense { get; set; } = string.Empty;
    public string Transportista { get; set; } = string.Empty;
    public string Medio { get; set; } = string.Empty;
    public string Consignee { get; set; } = string.Empty;
    public string SealNumber { get; set; } = string.Empty;
    public string? UpdatedByUserName { get; set; }
    public DateOnly? UpdatedDate { get; set; }
    public TimeOnly? UpdatedTime { get; set; }

    public DateOnly? MedioExitDate { get; set; }
    public TimeOnly? MedioExitTime { get; set; }

}

public class RecordEntranceItemDto
{
    public Guid Id { get; set; }
    public RecordEntranceStatus Status { get; set; }
    public bool IsConsolidated { get; set; }


    public ReceptionEntranceItemDto? ReceptionEntrance { get; set; }
    public StepExecutionLogItemDto? ExecutionLog { get; set; }
    public List<EntranceDucatItemDto> Ducats { get; set; } = [];
}
public class GetReceptionEntrancesDto
{
    public List<RecordEntranceItemDto> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    public ReceptionEntranceStatsDto Stats { get; set; } = new();
}