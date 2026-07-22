namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public class ReceptionEntranceStatsDto
{
    public int InTail { get; set; }
    public int InUnloading { get; set; }
    public int Completed { get; set; }
}

public class EntranceDucatItemDto
{
    public Guid EntranceDucatId { get; set; }
    public string DucatNumber { get; set; } = string.Empty;
}

public class ReceptionEntranceListItemDto
{
    public Guid RecordEntranceId { get; set; }
    public Guid ReceptionEntranceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CurrentStepCode { get; set; } = string.Empty;
    public bool IsConsolidated { get; set; }
    public DateTime CreatedAt { get; set; }

    public DateOnly ReceptionStartDate { get; set; }
    public TimeOnly ReceptionStartTime { get; set; }
    public DateOnly? ReceptionEndDate { get; set; }
    public TimeOnly? ReceptionEndTime { get; set; }

    public int? DurationTotalSeconds { get; set; }
    public string? durationFormatted { get; set; }

    public string CountryOfOrigin { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string TrailerChassis { get; set; } = string.Empty;
    public string DirverLicense { get; set; } = string.Empty;
    public string Transportista { get; set; } = string.Empty;
    public string Medio { get; set; } = string.Empty;
    public string Consignee { get; set; } = string.Empty;
    public string SealNumber { get; set; } = string.Empty;

    public List<EntranceDucatItemDto> Ducats { get; set; } = [];
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