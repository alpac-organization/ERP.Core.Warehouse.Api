namespace ERP.Core.Warehouse.Api.Application.Commons.Options
{
    public class PurchaseRequestOptions
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class ProcessPurchaseRequestOptions : PurchaseRequestOptions { }
}