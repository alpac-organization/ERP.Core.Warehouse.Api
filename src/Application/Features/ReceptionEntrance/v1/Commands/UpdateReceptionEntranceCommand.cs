using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

public class UpdateReceptionEntranceCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }

    public List<string> DucatNumbers { get; set; } = [];

    public string CountryOfOrigin { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string TrailerChassis { get; set; } = string.Empty;
    public string DriverLicense { get; set; } = string.Empty;
    public string Transportista { get; set; } = string.Empty;
    public string Medio { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string Consignee { get; set; } = string.Empty;
    public string SealNumber { get; set; } = string.Empty;

}