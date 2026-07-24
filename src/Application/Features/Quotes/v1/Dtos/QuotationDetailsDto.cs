namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos
{
    public class QuotationDetailsDto
    {
        public Guid QuotationDetailId { get; set; }
        public int Amount { get; set; }
        public string? Color { get; set; }
        public decimal IndividualPrice { get; set; }

        public string? Observations { get; set; }
        public string? AdditionalData { get; set; } = "{}";


        //Objetos ingnorados al hacer el mapping para mejor control de los datos
        public ProductDetailsDto ProductDetails { get; set; } = new ();
        public SupplierDetailsDto SupplierDetails { get; set; } = new ();
        public UnitMeasureDatailsDto UnitMeasureDatails { get; set; } = new ();
    }
}