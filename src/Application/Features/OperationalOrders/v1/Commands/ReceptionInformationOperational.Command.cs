using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Commands
{
    public class ReceptionInformationOperationalCommand : BaseRequest, IRequest<Unit>
    {
        public Guid OperationalOrderId { get; set; }

        //Información de recepción...
        public Guid CustomerId { get; set; }
        public decimal PackageAmount { get; set; }
        public decimal MerchandiseWeight { get; set; }

        public bool HasMerchandise { get; set; }
        public MerchandiseInformation? MerchandiseInformation { get; set; }
    }

    public class MerchandiseInformation
    {
        public string? Merchandise { get; set; }
        public string? MerchandiseDescription { get; set; }
    }
}