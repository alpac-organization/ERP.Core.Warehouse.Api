namespace ERP.Core.Warehouse.Api.Domain.Entities.ObjectValues
{
    public class DocumentSignatureInfo
    {
        public string? RequestedBy { get; set; }
        public string? ApprovedBy { get; set; }
        public string? AuthorizedBy { get; set; }
        public string? Bank { get; set; }
    }
}
