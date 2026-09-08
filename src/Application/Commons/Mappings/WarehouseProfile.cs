using AutoMapper;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using Commands = ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class WarehouseProfile : Profile
{
   public WarehouseProfile()
   {
      CreateMap<Warehouses, WarehouseDto>();
      CreateMap<WarehouseCapacity, WarehouseCapacityDto>();   
      // CreateMap<WarehouseCapacity, WarehouseCapacityDto>();   
      //Your mapper location here 
   }
}

public static class WarehouseMapper
{
   public static Warehouses ToWarehouseEntity(this Commands.RegisterWarehouseCommand command)
   {
      
      return new ();
   }
}
