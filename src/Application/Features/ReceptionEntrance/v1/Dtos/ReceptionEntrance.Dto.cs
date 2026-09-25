namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos
{
    public class ReceptionEntranceDto
    {
        public Guid ReceptionEntranceId { get; set; }

        public string SealNumber { get; set; } = null!;
        public string ContainerNumber { get; set; } = null!;
        public string CountryOfOrigin { get; set; } = null!;
        
        public DateTime CreatedAt { get; set; }
    }
}