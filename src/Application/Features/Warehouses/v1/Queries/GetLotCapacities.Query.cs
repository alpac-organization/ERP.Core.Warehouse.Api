using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries
{
   public class GetLotCapacitiesQuery : BaseRequest, IRequest<LotCapacitiesDto>
   {
      public Guid WarehouseId { get; set; }
      public Guid SectionId { get; set; }
      public Guid LotId { get; set; }
   }
}