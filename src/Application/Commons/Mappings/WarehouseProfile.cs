using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using Commands = ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class WarehouseProfile : Profile
{
   public WarehouseProfile()
   {
      CreateMap<Warehouses, WarehouseDto>()
         .ForMember(dest => dest.WarehouseId, opt => opt.MapFrom(src => src.Id));

      CreateMap<WarehouseCapacity, WarehouseCapacitiesDto>();

      CreateMap<WarehouseLocation, WarehouseLocationDto>();

      CreateMap<Sections, SectionSummaryDto>()
         .ForMember(dest => dest.SectionId, opt => opt.MapFrom(src => src.Id));

      CreateMap<Warehouses, WarehouseDetailDto>()
         .ForMember(dest => dest.WarehouseId, opt => opt.MapFrom(src => src.Id))
         .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.WarehouseLocation))
         .ForMember(dest => dest.Capacity, opt => opt.MapFrom(src => src.WarehouseCapacity))
         .ForMember(dest => dest.Sections, opt => opt.MapFrom(src => src.Sections));

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
      
      return new()
      {
         Id            = Guid.NewGuid(),
         Code          = command.Code.Trim(),
         WarehouseType = command.WarehouseType,
         IsActive      = true
      };
   }
   public static WarehouseLocation ToWarehouseLocation(this Commands.RegisterWarehouseLocation command,Guid company_id,Guid warehouse_id)
   {
      return new()
      {
         Id           = Guid.NewGuid(),
         IsActive     = true,
         CompanyId    = company_id,
         LocationName = command.LocationName,
         WarehouseId  = warehouse_id 
      };   
   }

       public static WarehouseCapacity ToWarehouseCapacityEntity(
        this  Commands.RegisterWarehouseCommand command,
        Guid warehouseId,
        WarehouseCapacity calculated)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Width = command.Width,
            Length = command.Length,
            HasMargins = command.HasMargins,
            MinimumHeight = command.MinimumHeight,
            MaximumHeight = command.MaximumHeight,
            MarginTop = command.MarginTop,
            MarginBottom = command.MarginBottom,
            MarginLeft = command.MarginLeft,
            MarginRight = command.MarginRight,

            TotalAreaM2 = calculated.TotalAreaM2,
            UnusedAreaM2 = calculated.UnusedAreaM2,
            AvailableAreaWithMarginM2 = calculated.AvailableAreaWithMarginM2,
            OccupiedChargeableAreaM2 = calculated.OccupiedChargeableAreaM2,
            UnoccupiedChargeableAreaM2 = calculated.UnoccupiedChargeableAreaM2,
            PercentageAvailableAreaWithMarginM2 = calculated.PercentageAvailableAreaWithMarginM2,
            TotalVolumenM3 = calculated.TotalVolumenM3,
            UnusedVolumenM3 = calculated.UnusedVolumenM3,
            AvailableVolumenWithMarginM3 = calculated.AvailableVolumenWithMarginM3,
            OccupiedChargeableVolumenM3 = calculated.OccupiedChargeableVolumenM3,
            UnoccupiedChargeableVolumenM3 = calculated.UnoccupiedChargeableVolumenM3,
            PercentageAvailableVolumenWithMarginM3 = calculated.PercentageAvailableVolumenWithMarginM3
        };
    }
}