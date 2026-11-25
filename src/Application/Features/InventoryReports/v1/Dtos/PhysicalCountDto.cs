using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos
{
    public class CreatePhysicalCountResponse
    {
        public Guid PhysicalCountId { get; set; }
    }

    public class CreatePhysicalCountRequest
    {
        public string? Notes { get; set; }
    }

    public class PhysicalCountListItemDto
    {
        public Guid PhysicalCountId { get; set; }
        public PhysicalCountStatus Status { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TotalLines { get; set; }
    }

    public class PhysicalCountSummaryDto
    {
        public Guid PhysicalCountId { get; set; }
        public Guid CompanyId { get; set; }
        public Guid BranchId { get; set; }
        public string? BranchName { get; set; }

        public PhysicalCountStatus Status { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public string? CreatedByName { get; set; }

        public DateTime? CompletedAt { get; set; }
        public Guid? CompletedBy { get; set; }
        public string? CompletedByName { get; set; }

        public int TotalLines { get; set; }
        public int CompletedLines { get; set; }
        public int PendingLines => TotalLines - CompletedLines;
        public bool CanPrint => TotalLines > 0 && CompletedLines >= TotalLines;
    }

    public class PhysicalCountLinesPageDto
    {
        public List<PhysicalCountLineDto> Data { get; set; } = [];
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public PhysicalCountSummaryDto PhysicalCount { get; set; } = new();
    }

    public class PhysicalCountLineDto
    {
        public Guid PhysicalCountLineId { get; set; }
        public Guid PhysicalCountId { get; set; }
        public Guid ProductStockId { get; set; }

        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }

        public decimal SystemStock { get; set; }
        public decimal? PhysicalStock { get; set; }
        public decimal? Difference { get; set; }

        public string? Observations { get; set; }

        public string? CountedByName { get; set; }
        public DateTime? CountedAt { get; set; }
    }

    public class UpdatePhysicalCountLineSource
    {
        public decimal? PhysicalStock { get; set; }
        public string? Observations { get; set; }
        public Guid UserId { get; set; }
        public DateTime CountedAt { get; set; }
    }
}
