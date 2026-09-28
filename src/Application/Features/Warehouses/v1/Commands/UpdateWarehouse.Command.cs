using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands
{
   public class UpdateWarehouseCommand :BaseRequest, IRequest<bool>
   {
      [JsonIgnore]
      public Guid WarehouseId {get; set;}

      // public 
   }
}