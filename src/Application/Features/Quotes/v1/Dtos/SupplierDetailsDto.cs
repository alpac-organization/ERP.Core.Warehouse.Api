using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos
{
    public class SupplierDetailsDto
    {
        public Guid SupplierId { get; set; }
        public string? SupplierLegalName { get; set; }
        public string? IdentificationNumber { get; set; }
        public IdentificationType? IdentificationType { get; set; }
        public string? ContactPhoneNumber { get; set; }
        public ConstitutionType ConstitutionType { get; set; }        
    }
}