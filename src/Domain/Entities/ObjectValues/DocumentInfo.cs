namespace ERP.Core.Warehouse.Api.Domain.Entities.ObjectValues
{
    public class DocumentInfo
    {
        public string? Title { get; set; }
        public string? RequestCode { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Date { get; set; }
        public string? AssignmentNumber { get; set; }
        public bool IsNormal { get; set; }
        public bool IsCritical { get; set; }
        public string? AdministrativeFineNumber { get; set; }
        public string? DeclarationNumber { get; set; }
        public int QuoteCount { get; set; }
    }
}
