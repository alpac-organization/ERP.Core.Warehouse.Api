using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos
{
    public class OperationalOrderDetailsDto : OperationalOrderDto
    {
        public string? ShippingCompany { get; set; }
        public string? Consignee { get; set; }
        public string? Sender { get; set; }
        public decimal? Weight { get; set; }
        public decimal? PackagesCount { get; set; }
        public string? Description { get; set; }
        public string? PolicyNumber { get; set; }


        //Información de recepción de alpac.
        public CustomerDto? CustomerInformation { get; set; }
        public CostCenterInformation? CostCenterInformation { get; set; }
        public ReceptionEntranceDetailsDto? ReceptionEntranceInformation { get; set; }
    }

}