using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using Commands = ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
   public class SectionProfile : Profile
   {
      public SectionProfile()
      {
         CreateMap<Sections, SectionDto>()
            .ForMember(dest => dest.SectionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SectionCode, opt => opt.MapFrom(src => src.Code));

         CreateMap<SectionCapacity, SectionCapacityDto>()
            .ForMember(dest => dest.SectionCapacityId, opt => opt.MapFrom(src => src.Id));

         CreateMap<Sections, SectionDetailsDto>()
            .ForMember(dest => dest.SectionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SectionCode, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.Capacity, opt => opt.MapFrom(src => src.SectionCapacity))
            .ForMember(dest => dest.Coordinates, opt => opt.Ignore());
      }
   }

   public static class SectionMapper
   {
      public static Sections ToSectionEntity(this Commands.RegisterSectionCommand command)
      {
         return new()
         {
            Id = Guid.NewGuid(),
            Code = command.Code,
            WarehouseId = command.WarehouseId,
            SectionType = command.SectionType,
            IsActive = true
         };
      }

      public static SectionCapacity ToSectionCapacityEntity(this Commands.RegisterSectionCommand command, Guid SectionId, SectionCapacity sectionCapacity)
      {
         return new()
         {
            Id = Guid.NewGuid(),
            SectionId = SectionId,
            Length = command.Length,
            Width = command.Width,
            TotalAreaM2 = sectionCapacity.TotalAreaM2,
            UnusedAreaM2 = sectionCapacity.UnusedAreaM2,
            AvailableAreaWithMarginM2 = sectionCapacity.AvailableAreaWithMarginM2,
            OccupiedChargeableAreaM2 = sectionCapacity.OccupiedChargeableAreaM2,
            UnoccupiedChargeableAreaM2 = sectionCapacity.UnoccupiedChargeableAreaM2,
            PercentageAvailableAreaWithMarginM2 = sectionCapacity.PercentageAvailableAreaWithMarginM2
         };
      }
   }
}