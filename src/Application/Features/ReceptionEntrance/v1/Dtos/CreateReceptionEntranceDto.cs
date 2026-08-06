using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public record CreateReceptionEntranceDto
{
    public DocumentType DocumentType { get; set; }
    public List<string> DucatNumbers { get; set; } = [];

    public string? CustomsDeclarationNumber { get; set; }
    public int? Packages { get; set; }
    public string? Customer { get; set; }
    public string? Product { get; set; }
    public string? ContainerNumber { get; set; }

    public string CountryOfOrigin { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string TrailerChassis { get; set; } = string.Empty;
    public string DriverLicense { get; set; } = string.Empty;
    public string Transportista { get; set; } = string.Empty;
    public Guid TransportUnitId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string SealNumber { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public TimeOnly StartTime { get; set; }
}