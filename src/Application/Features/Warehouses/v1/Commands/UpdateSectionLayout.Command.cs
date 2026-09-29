using System.Text.Json.Serialization;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class UpdateSectionLayoutCommand : BaseRequest, IRequest<bool>
{
   [JsonIgnore]
   public Guid WarehouseId { get; set; }
   [JsonIgnore]
   public Guid SectionId { get; set; }
   public decimal? PositionX { get; set; }
   public decimal? PositionY { get; set; }
   public decimal? Width { get; set; }
   public decimal? Length { get; set; }
}