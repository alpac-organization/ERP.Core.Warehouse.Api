using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries
{
   public class GetLotCapacitiesQuery : BaseRequest, IRequest<LotCapacitiesDto>
   {
      public Guid WarehouseId { get; set; }
      public Guid SectionId { get; set; }
      public Guid LotId { get; set; }
   }
}