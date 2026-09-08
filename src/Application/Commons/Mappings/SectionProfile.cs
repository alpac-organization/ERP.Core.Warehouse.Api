

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

      public static SectionCapacity ToSectionCapacityEntity()
      {
         return new()
         {
            Witdh = 
         };
      }
   }
}