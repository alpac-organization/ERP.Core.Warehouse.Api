using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos
{
    public class OperationalOrderDetailsDto : OperationalOrderDto
    {
        public string? Description { get; set; }
        public string? PolicyNumber { get; set; }

        public DocumentType DocumentType { get; set; }

        public decimal? Weight { get; set; }
        public decimal? PackagesCount { get; set; }

        //Información de recepción de alpac.
        public WarehouseInformation? WarehouseInformation { get; set; }
        public ReceptionEntranceDetailsDto? ReceptionEntranceInformation { get; set; }
    }

    public class WarehouseInformation
    {
        public Guid WarehouseId { get; set; }        
        public string Code { get; set; } = string.Empty;
        public WarehouseType WarehouseType { get; set; }        
    }
}