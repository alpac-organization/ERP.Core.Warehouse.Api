namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos.Documents
{
    public class PurchaseOrderDocumentTemplateDto
    {
        public string? LogoUrl { get; set; }
        public string? Initials { get; set; }
        public string? CompanyName { get; set; }
        public string? Title { get; set; }
        public string? RequestCode { get; set; }
        public string? Date { get; set; }
        public string? Department { get; set; }
        public string? Payee { get; set; }
        public string? Concept { get; set; }
        public string? Customer { get; set; }
        public string? CompanyRuc { get; set; }
        public string? Customs { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? AssignmentNumber { get; set; }

        public decimal ServiceAmount { get; set; }
        public decimal ExemptServiceAmount { get; set; }
        public decimal OtherDisbursement { get; set; }
        public decimal Vat { get; set; }
        public decimal IncomeTax { get; set; }
        public decimal MunicipalTax { get; set; }
        public decimal Others { get; set; }
        public decimal NetToPay { get; set; }

        public bool IsNormal { get; set; }
        public bool IsCritical { get; set; }
        public string? AdministrativeFineNumber { get; set; }
        public string? DeclarationNumber { get; set; }
        public int QuoteCount { get; set; }

        public string? RequestedBy { get; set; }
        public string? ApprovedBy { get; set; }
        public string? AuthorizedBy { get; set; }
        public string? Bank { get; set; }
    }
}
