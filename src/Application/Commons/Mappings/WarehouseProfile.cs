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
            var warehouseId = Guid.NewGuid();

            var details = new WarehouseDetails
            {
                Id = Guid.NewGuid(),
                WarehouseId         = warehouseId,
                WitdhMetres         = command.WarehouseDetails.WidthMetres,
                LengthMetres        = command.WarehouseDetails.LengthMetres,
                RampsCount          = command.WarehouseDetails.RampsCount,
                ParkingSpacesCount  = command.WarehouseDetails.ParkingSpacesCount
            };

            return new()
            {
                Id                  = warehouseId,
                Code                = command.Code,
                IsActive            = true,
                WarehouseName       = command.WarehouseName,
                BranchId            = command.BranchId,
                WarehouseType       = command.WarehouseType,
                ParentWarehouseId   = command.ParentWarehouseId,

                Details             = details,
                Capacity            = details.ToCalculatedCapacity(warehouseId)
            };
        }

        public static WarehouseCapacity ToCalculatedCapacity(this WarehouseDetails details, Guid warehouseId)
        {
            var totalArea   = details.WitdhMetres * details.LengthMetres;

            return new WarehouseCapacity
            {
                Id                      = Guid.NewGuid(),
                WarehouseId             = warehouseId,
                TotalAreaM2             = totalArea,
                UsableAreaM2            = null, // aún no hay secciones/racks para descontar
                UnusableAreaM2          = null,
                TotalMaxPolines         = null,
                CurrentPolinesStored    = null,
                LastCalculatedAt        = DateTime.UtcNow
            };
        }

        public static Sections ToSectionEntity(this Commands.SectionInformation command, Guid warehouseId, string sectionCode)
        {
            return new()
            {
                // IsActive = true,
                // Code = sectionCode,
                // Id = Guid.NewGuid(),
                // Name = command.ZoneName,
                // WarehouseId = warehouseId,
                // LengthMetres = command.LengthMetres,
                // HeightMetres = command.HeightMetres,
                // MaxWeightCapacityKg = command.MaxWeightCapacityKg,
                // TotalVolumeCapacityM3 = command.TotalVolumeCapacityM3,
            };
        }

        public static Racks ToRackEntity(this Commands.RackInformation command, Guid zoneId)
        {
            return new()
            {
                // IsAvailable = true,
                // SectionId = zoneId,
                // RowNumber = command.RowNumber,
                // LevelNumber = command.LevelNumber,
                // CostPerPosition = command.CostPerPosition,
                // MaxWeightKg = command.MaxWeightKg,
                // MaxHeightMetres = command.MaxHeightMetres
            };
        }
    }
}