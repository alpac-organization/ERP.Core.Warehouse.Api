using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos
{
    public class MonthlyPurchaseReportDto
    {
        public List<MonthlyPurchaseReportItemDto> Items { get; set; } = [];

        public decimal GrandTotal { get; set; }
    }

    public class MonthlyPurchaseReportItemDto
    {
        public int Month { get; set; }

        public int Year { get; set; }

        public string? Key { get; set; }

        public PurchaseRequestType RequestType { get; set; }

        public string? BranchName { get; set; }

        public string? AreaName { get; set; }

        public string? SupplierName { get; set; }

        public string? Description { get; set; }

        public int Quantity { get; set; }

        public Currency? Currency { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }
    }
}