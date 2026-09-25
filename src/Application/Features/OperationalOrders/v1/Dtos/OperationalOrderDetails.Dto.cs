using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos
{
    public class OperationalOrderDetailsDto : OperationalOrderDto
    {
        public string? Description { get; set; }
        public DocumentType DocumentType { get; set; }

        public decimal? Weight { get; set; }
        public decimal? PackagesCount { get; set; }
        
        public BranchInformation BranchInformation { get; set; } = new();

        //Información de recepción de alpac.
        public ReceptionEntranceDto ReceptionEntranceInformation { get; set; } = new();
    }

}