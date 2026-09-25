using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands
{
    public class RegisterLotsCommand : BaseRequest, IRequest<bool>
    {
        [JsonIgnore]
        public Guid WarehouseId { get; set; }

        [JsonIgnore]
        public Guid SectionId { get; set; }

        public int Quantity { get; set; }

        public int? NominalRows { get; set; }

        public int? NominalColumns { get; set; }

        public decimal Width { get; set; }

        public decimal Length { get; set; }
    }
}