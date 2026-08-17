using ERP.Core.Database.Domain.Enums;

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
    public Guid? CustomBranchId { get; set; }
    public string? VehiclePlateNumber { get; set; }
    public string? VehicleChassisNumber { get; set; }
    public string? ContainerNumber { get; set; }
    public string? DriverLicense { get; set; }
    public string? Transportista { get; set; }
    public TransportUnit? TransportUnit { get; set; }
    public string? DriverName { get; set; }
    public string? SealNumber { get; set; }

    public string? CustomsDeclarationNumber { get; set; }
    public int? Packages { get; set; }
    public string? Customer { get; set; }
    public string? Product { get; set; }
}