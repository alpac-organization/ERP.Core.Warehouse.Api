using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos
{

    public class QuotationInformationDto : QuotationDto
    {
        public List<QuotationDetailsDto> QuotedSuppliers { get; set; } = [];
    }


    public class QuotationDetailsDto
    {
        public Guid QuotationDetailId { get; set; }
        // public QuotationStatus Status { get; set; }

        //Objetos ingnorados al hacer el mapping para mejor control de los datos
        public SupplierDto SupplierInformation { get; set; } = new ();
        public List<QuotedProductDto> QuotedProducts { get; set; } = [];
    }

}