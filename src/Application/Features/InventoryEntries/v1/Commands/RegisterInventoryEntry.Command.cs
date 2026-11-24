using System.Text.Json.Serialization;
using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Commands
{
    public class RegisterInventoryEntryCommand : BaseRequest, IRequest<InventoryEntryDto>
    {
        [JsonIgnore]
        public Guid InventoryEntryId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly? InvoiceDate { get; set; }
        public string? Observations { get; set; }

        public List<string> EvidenceImagesBase64 { get; set; } = [];
        public List<RegisterInventoryEntryItem>? Items { get; set; }
    }

    public class RegisterInventoryEntryItem
    {
        public Guid PurchaseOrderItemId { get; set; }
        public Guid? CostCenterId { get; set; }
    }
}
