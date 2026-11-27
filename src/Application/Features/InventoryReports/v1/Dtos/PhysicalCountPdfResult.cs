namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos
{
    public sealed class PhysicalCountPdfResult
    {
        public byte[] Bytes { get; set; } = [];
        public DateTime CreatedAt { get; set; }
    }
}
