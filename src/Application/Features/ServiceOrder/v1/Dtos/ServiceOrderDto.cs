using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos
{
    public class ServiceOrderDto
    {
        public Guid ServiceOrderId { get; set; }

        public string Code { get; set; } = null!;

        public OSStatus Status { get; set; }

        public string? Observations { get; set; }

        public CustomerInformation? Customer { get; set; }
    }
    
    public class CustomerInformation
    {
        public Guid CustomerId { get; set; }

        public string? Cif { get; set; }

        public string? LegalName { get; set; }

        public string? PictureUrl { get; set; }

        public string? IdentificationNumber { get; set; }

        public IdentificationType IdentificationType { get; set; }
    }
}
