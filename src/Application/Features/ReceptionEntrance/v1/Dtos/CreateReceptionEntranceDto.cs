using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public record CreateReceptionEntranceDto
{
    public DocumentType DocumentType { get; set; }
    public string CountryOfOrigin { get; set; } = string.Empty;
    public Guid CustomBranchId { get; set; }
    public string VehiclePlateNumber { get; set; } = string.Empty;
    public string VehicleChassisNumber { get; set; } = string.Empty;
    public string? ContainerNumber { get; set; }
    public string DriverLicense { get; set; } = string.Empty;
    public string Transportista { get; set; } = string.Empty;
    public TransportUnit TransportUnit { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string SealNumber { get; set; } = string.Empty;
    public SealEvidenceDto? SealEvidence { get; set; }

    public List<string> DucatNumbers { get; set; } = [];

    public string? CustomsDeclarationNumber { get; set; }
    public int? Packages { get; set; }
    public string? Customer { get; set; }
    public string? Product { get; set; }

    public DateOnly StartDate { get; set; }
    public TimeOnly StartTime { get; set; }
}

public class SealEvidenceDto
{
    public string ImageBase64 { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}