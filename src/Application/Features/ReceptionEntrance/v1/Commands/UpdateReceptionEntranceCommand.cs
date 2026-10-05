using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;
using System.Text.Json.Serialization;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands
{
    public class UpdateReceptionEntranceCommand : BaseRequest, IRequest<Unit>
    {
        [JsonIgnore]
        public Guid ReceptionEntranceId { get; set; }

        public GeneralInformationUpdated? GeneralInformation { get; set; }
        public ReceptionTransportInformation? ReceptionTransportInformation { get; set; }

        //Agregar Nuevas Evidencias, a la lista        
        public List<string> EvidenceBase64 { get; set; } = [];

        //Eliminación de evidencias
        public List<Guid> EvidenceIdsToDelete { get; set; } = [];
    }

    public class GeneralInformationUpdated
    {
        public Guid CustomBranchId { get; set; }
        public string? SealNumber { get; set; }
        public string? CountryOrigin { get; set; }
        public string? ContainerNumber { get; set; }
        public DocumentType? DocumentType { get; set; }

        public List<DucatNumbersUpdate> DucatNumbers { get; set; } = [];
        public string? CustomsDeclarationNumber { get; set; }
    }

    public class DucatNumbersUpdate
    {
        public Guid OperationalOrderId { get; set; }
        public string DocumentNumber { get; set; } = default!;
    }

    public class ReceptionTransportInformation
    {
        public string? DriverName { get; set; }
        public string? DriverLicense { get; set; }
        public string? Transportista { get; set; }
        public string? VehiclePlateNumber { get; set; }
        public string? VehicleChassisNumber { get; set; }
        public TransportUnit? TransportUnit { get; set; }
    }
}