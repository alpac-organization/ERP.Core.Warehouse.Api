using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos
{
    public class OutputLineEquipmentReportDto
    {
        public DateTime GeneratedAt { get; set; }
        public string? GeneratedBy { get; set; }

        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public List<OutputLineEquipmentCategoryDto> Categories { get; set; } = [];
    }

    public class OutputLineEquipmentCategoryDto
    {
        public string? CategoryName { get; set; }

        public List<OutputLineEquipmentEquipmentDto> Equipments { get; set; } = [];
    }

    public class OutputLineEquipmentEquipmentDto
    {
        public EquipmentInformation EquipmentInformation { get; set; } = default!;
        public List<OutputLineEquipmentItemDto> Items { get; set; } = [];

        public decimal TotalQuantity { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalTotalCost { get; set; }
    }

    public class OutputLineEquipmentItemDto
    {
        public string? OutputCode { get; set; }
        public string? RequestCode { get; set; }
        public DateTime OutputDate { get; set; }
        public string? Supply { get; set; }
        public decimal Quantity { get; set; }
        public decimal Cost { get; set; }
        public decimal TotalCost { get; set; }
        public string? CostCenter { get; set; }
        public string? ServiceOrderCode { get; set; }
        public string? Customer { get; set; }
    }
}