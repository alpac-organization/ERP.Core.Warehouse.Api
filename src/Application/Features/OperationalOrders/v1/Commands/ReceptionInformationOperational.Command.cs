using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Commands
{
    public class ReceptionInformationOperationalCommand : BaseRequest, IRequest<Unit>
    {
        public Guid OperationalOrderId { get; set; }

        //Información de recepción...
        public Guid? CustomerId { get; set; }
        public decimal? PackageAmount { get; set; }
        public decimal? MerchandiseWeight { get; set; }
        public string? ShippingCompany { get; set; }
        public string? Consignee { get; set; }
        public string? Sender { get; set; }
        public bool? IsAlerted { get; set; } = false;

        public List<MerchandiseCreate>? Merchandises { get; set; }
    }

    public class MerchandiseCreate
    {
        public string? Merchandise { get; set; }
        public string? MerchandiseDescription { get; set; }
    }
}