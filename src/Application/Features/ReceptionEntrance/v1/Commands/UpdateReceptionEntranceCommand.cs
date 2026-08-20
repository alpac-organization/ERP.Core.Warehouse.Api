using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

public class UpdateReceptionEntranceCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }

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

    public List<string>? EvidenceToDelete { get; set; }
    public List<string>? EvidenceToAdd { get; set; }

    public string? CustomsDeclarationNumber { get; set; }
    public int? Packages { get; set; }
    public string? Customer { get; set; }
    public string? Product { get; set; }

}