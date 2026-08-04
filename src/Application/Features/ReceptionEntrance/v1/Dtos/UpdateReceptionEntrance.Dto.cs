namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public record UpdateDucatItemDto
{
    public Guid? Id { get; set; }
    public string DucatNumber { get; set; } = string.Empty;
}
public record UpdateReceptionEntranceDto
{
    public List<UpdateDucatItemDto>? Ducats { get; set; }

    public string? CountryOfOrigin { get; set; }
    public string? Aduana { get; set; }
    public string? PlateNumber { get; set; }
    public string? TrailerChassis { get; set; }
    public string? DriverLicense { get; set; }
    public string? Transportista { get; set; }
    public Guid? TransportUnitId { get; set; }
    public string? DriverName { get; set; }
    public string? SealNumber { get; set; }

    public string? CustomsDeclarationNumber { get; set; }
    public int? Packages { get; set; }
    public string? Customer { get; set; }
    public string? Product { get; set; }
    public string? ContainerNumber { get; set; }
}