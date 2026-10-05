using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos
{
    public class ReceptionEntranceDto
    {
        public Guid ReceptionEntranceId { get; set; }
        public string? ReceptionCode { get; set; }

        public string VehiclePlateNumber { get; set; } = null!;
        public string ContainerNumber { get; set; } = null!;
        public string CountryOfOrigin { get; set; } = null!;
        public string SealNumber { get; set; } = null!;
        public DocumentType DocumentType { get; set; }
        public TimeOnly? VehicleExitTime { get; set; }
        public TimeOnly? ContainerExitTime { get; set; }
    }
}