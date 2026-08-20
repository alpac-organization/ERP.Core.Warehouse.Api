namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public class DeletedEvidenceListItemDto
{
    public Guid ReceptionId { get; set; }
    public string CountryOfOrigin { get; set; } = string.Empty;
    public string CustomBranch { get; set; } = string.Empty;
    public string VehiclePlateNumber { get; set; } = string.Empty;
    public string VehicleChassisNumber { get; set; } = string.Empty;
    public string ContainerNumber { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public List<string> DeletedEvidenceUrls { get; set; } = [];
    public string? UpdatedByUserName { get; set; }
    public DateOnly? UpdatedDate { get; set; }
    public TimeOnly? UpdatedTime { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class GetDeletedEvidencesDto
{
    public List<DeletedEvidenceListItemDto> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}