

using System.Net;
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
         CreateMap<Sections, SectionDto>();
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

      public static SectionCapacity ToSectionCapacityEntity(
         this Commands.RegisterSectionCommand command, Guid SectionId,
         SectionCapacity sectionCapacity
      )
      {         
         return new()
         {
            Id = Guid.NewGuid(),
            SectionId = SectionId,
            Length = command.Length,
            Witdh = command.Width,
            UsableAreaM2 = sectionCapacity.UsableAreaM2,
            UnusableAreaM2 = sectionCapacity.UnusableAreaM2,
            AvailableSpaceWithSpacingM2 = sectionCapacity.AvailableSpaceWithSpacingM2,
            AvailableSpaceWithoutSpacingM2 = sectionCapacity.AvailableSpaceWithoutSpacingM2,
            PercenteAvailableSpaceWithSpacingM2 = sectionCapacity.PercenteAvailableSpaceWithSpacingM2,
            PercenteAvailableSpaceWithSpacingM3 = sectionCapacity.PercenteAvailableSpaceWithSpacingM3
         };
      }
   }
}