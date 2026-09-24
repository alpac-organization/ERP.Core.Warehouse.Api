using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands
{
    public class CreateReceptionEntranceCommand : BaseRequest, IRequest<Unit>
    {
        public List<string> EvidenceBase64 { get; set; } = [];

        public required GeneralInformation GeneralInformation { get; set; } = new();
        public required TransportInformation TransportInformation { get; set; } = new();
        public CustomsDeclarationInformation? CustomsDeclarationInformation { get; set; }
    }

    public class TransportInformation
    {
        public string DriverName { get; set; } = default!;
        public string DriverLicense { get; set; } = default!;
        public string Transportista { get; set; } = default!;
        public string VehiclePlateNumber { get; set; } = default!;
        public string VehicleChassisNumber { get; set; } = default!;
        public TransportUnit TransportUnit { get; set; }        
    }

    public class GeneralInformation
    {
        public Guid CustomBranchId { get; set; }
        
        public string SealNumber { get; set; } = default!;
        public string CountryOrigin { get; set; } = default!;
        public string ContainerNumber { get; set; } = default!;
        public DocumentType DocumentType { get; set; }

        public List<string> DucatNumbers { get; set; } = [];
        public string? CustomsDeclarationNumber { get; set; }
    }

    public class CustomsDeclarationInformation
    {
        public decimal TotalWeight { get; set; }
        public decimal PackageNumber { get; set; }
        public string? ProductDescription { get; set; }
    }
}