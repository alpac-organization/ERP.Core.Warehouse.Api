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
      // Actualizacion desde Lots
      CreateMap<WarehouseCapacity, WarehouseCapacity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.WarehouseId, opt => opt.Ignore())
            .ForMember(dest => dest.Warehouse, opt => opt.Ignore()); 
   }
}

public static class WarehouseMapper
{
   public static Warehouses ToWarehouseEntity(this Commands.RegisterWarehouseCommand command)
   {
      return new();
   }
}