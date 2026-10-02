using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Enums;
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
            .ForMember(dest => dest.SectionCode, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Width, opt => opt.MapFrom(src => src.SectionCapacity != null ? src.SectionCapacity.Width : 0))
            .ForMember(dest => dest.Length, opt => opt.MapFrom(src => src.SectionCapacity != null ? src.SectionCapacity.Length : 0))
            .ForMember(dest => dest.TotalArea, opt => opt.MapFrom(src => src.SectionCapacity != null ? src.SectionCapacity.TotalAreaM2 : 0))
            .ForMember(dest => dest.AvailableArea, opt => opt.MapFrom(src => src.SectionCapacity != null ? src.SectionCapacity.AvailableAreaWithMarginM2 : 0))
            .ForMember(dest => dest.PercentageAvailableArea, opt => opt.MapFrom(src => src.SectionCapacity != null ? src.SectionCapacity.PercentageAvailableAreaWithMarginM2 : 0))
            .ForMember(dest => dest.PositionX, opt => opt.MapFrom(src => src.SectionCoordinates != null ? (decimal?)src.SectionCoordinates.PositionX : null))
            .ForMember(dest => dest.PositionY, opt => opt.MapFrom(src => src.SectionCoordinates != null ? (decimal?)src.SectionCoordinates.PositionY : null))
            .ForMember(dest => dest.PositionZ, opt => opt.MapFrom(src => src.SectionCoordinates != null ? (decimal?)src.SectionCoordinates.PositionZ : null))
            .ForMember(dest => dest.RotationY, opt => opt.MapFrom(src => src.SectionCoordinates != null ? (decimal?)src.SectionCoordinates.RotationY : null));

         CreateMap<SectionCapacity, SectionCapacityDto>()
            .ForMember(dest => dest.SectionCapacityId, opt => opt.MapFrom(src => src.Id));

         CreateMap<SectionCoordinates, SectionCoordinatesDto>()
            .ForMember(dest => dest.SectionCoordinateId, opt => opt.MapFrom(src => src.Id));

         CreateMap<Sections, SectionDetailsDto>()
            .ForMember(dest => dest.SectionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SectionCode, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.Capacity, opt => opt.MapFrom(src => src.SectionCapacity))
            .ForMember(dest => dest.Coordinates, opt => opt.MapFrom(src => src.SectionCoordinates));

         CreateMap<SectionCapacity, SectionCapacity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SectionId, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Section, opt => opt.Ignore());
      }
   }

   public static class SectionMapper
   {
      public static Sections ToSectionEntity(this Commands.RegisterSectionCommand command, string code)
      {
         return new()
         {
            Id = Guid.NewGuid(),
            Code = code,
            WarehouseId = command.WarehouseId,
            SectionType = command.SectionType,
            SectionStorageType = command.SectionStorageType,
            MaxPalletsPerLevelAisle =
               command.SectionType == SectionType.Aisle
               && command.SectionStorageType == SectionStorageType.Pallets
                  ? command.MaximumNumberOfPalletsPerLevel
                  : null
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

      public static SectionCoordinates ToSectionCoordinateEntity(this Commands.RegisterSectionCommand command, Guid SectionId)
      {
         return new()
         {
            Id = Guid.NewGuid(),
            SectionId = SectionId,
            PositionX = command.PositionX,
            PositionY = command.PositionY,
            PositionZ = command.PositionZ,
            RotationY = command.RotationY
         };
      }
   }
}