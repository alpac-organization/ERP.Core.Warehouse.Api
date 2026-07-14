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
                IsActive = true,
                Name = command.WarehouseName,
                BranchId = command.BranchId,
                MaxHeight = command.WarehouseInformation.MaxHeight,
                MinHeight = command.WarehouseInformation.MinHeight,
                RampasCount = command.WarehouseInformation.RampasCount,
                WarehouseType = command.WarehouseInformation.WarehouseType,
                ParkingSpacesCount = command.WarehouseInformation.ParkingSpacesCount,
            };
        }

        public static RacksManagua ToRackEntity(this Commands.RackInformation command)
        {
            return new()
            {
                
            };
        }
    }
}