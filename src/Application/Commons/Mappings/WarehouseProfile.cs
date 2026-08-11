using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using Commands = ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{

    public class WarehouseProfile : Profile
    {
        public WarehouseProfile()
        {
            CreateMap<Warehouses, WarehouseDto>()
                .ForMember(dest => dest.WarehouseId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.WarehouseCode, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.WarehouseName));
        }
    }

    public static class WarehouseMapper
    {
        public static Warehouses ToWarehouseEntity(this Commands.RegisterWarehouseCommand command)
        {
            return new()
            {
                IsActive            = true,
                Id                  = Guid.NewGuid(),
                WarehouseName       = command.WarehouseName,
                BranchId            = command.BranchId,
                WarehouseType       = command.WarehouseInformation.WarehouseType
            };
        }

        public static Sections ToSectionEntity(this Commands.SectionInformation command, Guid warehouseId, string sectionCode)
        {
            return new()
            {
                IsActive              = true,
                Code                  = sectionCode,
                Id                    = Guid.NewGuid(),
                Name                  = command.ZoneName,
                WarehouseId           = warehouseId,
                WidthMetres           = command.WidthMetres,
                LengthMetres          = command.LengthMetres
            };
        }

        public static Racks ToRackEntity(this Commands.RackInformation command, Guid warehouseId)
        {
            return new()
            {
                IsAvailable     = true,
                WarehouseId     = warehouseId,
                RowNumber       = command.RowNumber,
                LevelNumber     = command.LevelNumber,
                CostPerPosition = command.CostPerPosition
            };
        }
    }
}