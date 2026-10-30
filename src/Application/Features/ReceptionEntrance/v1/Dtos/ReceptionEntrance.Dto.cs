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

        public class Stats
        {
            public int TotalEntries { get; set; }
            public int TotalsInPlant { get; set; }
            public int TotalDispatched { get; set; }
            public int ContainersOnSite { get; set; }
            public int ContainersDispatched { get; set; }
        }
    }
}