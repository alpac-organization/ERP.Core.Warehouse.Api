using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Dtos
{
    public class CustomerDto
    {
        public Guid CustomerId { get; set; }

        public string? Cif { get; set; }
        public string? LegalName { get; set; }
        public string? CustomerCode { get; set; }
        public string? IdentificationNumber { get; set; }
        public CustomerType CustomerType { get; set; }
        
        public IdentificationType IdentificationType { get; set; }
    }
}
