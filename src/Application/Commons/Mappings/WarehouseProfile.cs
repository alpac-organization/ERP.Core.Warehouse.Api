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
                MaxHeight           = command.WarehouseInformation.MaxHeight,
                MinHeight           = command.WarehouseInformation.MinHeight,
                RampasCount         = command.WarehouseInformation.RampasCount,
                WarehouseType       = command.WarehouseInformation.WarehouseType,
                ParkingSpacesCount  = command.WarehouseInformation.ParkingSpacesCount,
                TotalArea           = command.WarehouseInformation.TotalArea,
                TotalCubicCapacity  = command.WarehouseInformation.TotalCubicCapacity,
                UnusableArea        = command.WarehouseInformation.UnusableArea
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
                LengthMetres          = command.LengthMetres,
                HeightMetres          = command.HeightMetres,
                MaxWeightCapacityKg   = command.MaxWeightCapacityKg,
                TotalVolumeCapacityM3 = command.TotalVolumeCapacityM3,
            };
        }

        public static Racks ToRackEntity(this Commands.RackInformation command, Guid zoneId)
        {
            return new()
            {
                IsAvailable     = true,
                SectionId       = zoneId,
                RowNumber       = command.RowNumber,
                LevelNumber     = command.LevelNumber,
                CostPerPosition = command.CostPerPosition,
                MaxWeightKg     = command.MaxWeightKg,
                MaxHeightMetres = command.MaxHeightMetres  
            };
        }
    }
}