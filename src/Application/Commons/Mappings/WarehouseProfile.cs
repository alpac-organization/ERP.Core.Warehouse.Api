using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using Commands = ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public static class WarehouseMapper
    {
        public static Warehouses ToWarehouseEntity(this Commands.RegisterWarehouseCommand command)
        {
            return new()
            {
                IsActive            = true,
                Id                  = Guid.NewGuid(),
                Name                = command.WarehouseName,
                BranchId            = command.BranchId,
                MaxHeight           = command.WarehouseInformation.MaxHeight,
                MinHeight           = command.WarehouseInformation.MinHeight,
                RampasCount         = command.WarehouseInformation.RampasCount,
                WarehouseType       = command.WarehouseInformation.WarehouseType,
                ParkingSpacesCount  = command.WarehouseInformation.ParkingSpacesCount,
            };
        }

        public static Sections ToZoneEntity(this Commands.SectionInformation command, Guid warehouseId)
        {
            return new()
            {
                IsActive              = true,
                Id                    = Guid.NewGuid(),
                Name                  = command.ZoneName,
                WarehouseId           = warehouseId,
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

        public static Warehouses ToGalleysEntity(this Commands.WarehouseInformation command, Guid parentId)
        {
            return new()
            {
                // your another mappers
            };
        }
    }
}