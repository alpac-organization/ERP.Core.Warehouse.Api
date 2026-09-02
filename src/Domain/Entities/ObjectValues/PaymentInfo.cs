namespace ERP.Core.Warehouse.Api.Domain.Entities.ObjectValues
{
    public class PaymentInfo
    {
        public string? Payee { get; set; }
        public string? Customer { get; set; }
        public string? Department { get; set; }
        public string? Customs { get; set; }

        public decimal ServiceAmount { get; set; }
        public decimal ExemptServiceAmount { get; set; }
        public decimal OtherDisbursement { get; set; }
        public decimal Vat { get; set; }
        public decimal IncomeTax { get; set; }
        public decimal MunicipalTax { get; set; }
        public decimal Others { get; set; }
        public decimal NetToPay { get; set; }
    }
}
