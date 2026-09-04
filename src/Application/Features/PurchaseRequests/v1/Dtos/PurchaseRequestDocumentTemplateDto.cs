using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.ObjectValues;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos
{
    /// <summary>
    /// Modelo que se renderiza en la plantilla del documento (Handlebars).
    /// </summary>
    public class PurchaseRequestDocumentTemplateDto : DocumentBase
    {
        public CompanyInformation CompanyInformation { get; set; } = new();
        public DocumentInfo DocumentInfo { get; set; } = new();
        public List<PurchaseRequestDocumentArea> Areas { get; set; } = [];
    }

    public class PurchaseRequestDocumentArea
    {
        public string? AreaName { get; set; }
        public string? RequestCode { get; set; }
        public List<PurchaseRequestDocumentItem> Items { get; set; } = [];
    }

    public class PurchaseRequestDocumentItem
    {
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public int? QuantityUnit { get; set; }
        public string? UnitMeasure { get; set; }
        public string? Category { get; set; }
        public string? Justification { get; set; }
    }
}
