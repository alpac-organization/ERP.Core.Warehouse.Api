using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Commands
{
    public class ReceptionInformationOperationalCommand : BaseRequest, IRequest<Unit>
    {
        public Guid OperationalOrderId { get; set; }

        //Información de recepción...
        public Guid? CustomerId { get; set; }
        public string? Description { get; set; }
        public string? DocumentNumber { get; set; }
        public decimal? PackageAmount { get; set; }
        public decimal? MerchandiseWeight { get; set; }
        public string? ShippingCompany { get; set; }
        public string? Sender { get; set; }
        public bool? IsAlerted { get; set; }
        public bool? HasConsignees { get; set; }
        public bool? IsInTransit { get; set; }


        public List<MerchandiseCreate>? Merchandises { get; set; }
    }

    public class MerchandiseCreate
    {
        public string? Merchandise { get; set; }
        public string? MerchandiseDescription { get; set; }
    }
}