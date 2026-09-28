using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos
{
    public class ReceptionEntranceDetailsDto : ReceptionEntranceDto
    {
        public DateTime CreatedAt { get; set; }
        public string? AdditionalData { get; set; }

        public CustomBranchesInformation CustomBranchesInformation { get; set; } = new();
        public ReceptionTransportEntranceDto ReceptionTransportEntranceInformation { get; set; } = new();
    }

    public class ReceptionTransportEntranceDto
    {
        public string DriverName { get; set; } = null!;
        public string DriverLicense { get; set; } = null!;
        public string Transportista { get; set; } = null!;
        public string VehiclePlateNumber { get; set; } = null!;
        public string VehicleChassisNumber { get; set; } = null!;
        public TransportUnit TransportUnit { get; set; }
    }
}