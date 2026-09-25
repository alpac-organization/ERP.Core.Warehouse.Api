using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands
{
   public class DeleteSectionCommand : BaseRequest, IRequest<bool>
   {
      [JsonIgnore]
      public Guid WarehouseId { get; set; }

      [JsonIgnore]
      public Guid SectionId { get; set; }
   }
}