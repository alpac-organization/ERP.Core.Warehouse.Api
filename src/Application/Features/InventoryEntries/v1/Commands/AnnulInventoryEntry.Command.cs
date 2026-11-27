using System.Text.Json.Serialization;
using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Commands
{
    public class AnnulInventoryEntryCommand : BaseRequest, IRequest<InventoryEntryDto>
    {
        [JsonIgnore]
        public Guid InventoryEntryId { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
