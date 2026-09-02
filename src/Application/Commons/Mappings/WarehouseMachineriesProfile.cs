using System;
using AutoMapper;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class WarehouseMachineriesProfile : Profile
    {
        public WarehouseMachineriesProfile()
        {
            CreateMap<WarehouseMachinery, WarehouseMachineryListDto>()
                .ConstructUsing(m => new WarehouseMachineryListDto(
                    m.Id,
                    m.Code,
                    m.SerialNumber,
                    m.LicensePlate,
                    m.Name,
                    m.Brand,
                    m.Model,
                    m.MachineryType.ToString(),
                    m.FuelType.ToString(),
                    m.Status.ToString()
                ));
        }
    }

    public static class WarehouseMachineriesMapper
    {
        public static CreateWarehouseMachineryCommand ToCommand(
            this CreateWarehouseMachineryDto dto, Guid userId, Guid companyId, string moduleCode)
        {
            return new CreateWarehouseMachineryCommand
            {
                UserId = userId,
                CompanyId = companyId,
                ModuleCode = moduleCode,
                BranchId = dto.BranchId,
                WarehouseId = dto.WarehouseId,
                AssignedOperatorId = dto.AssignedOperatorId,
                Code = dto.Code,
                SerialNumber = dto.SerialNumber,
                LicensePlate = dto.LicensePlate,
                Name = dto.Name,
                Brand = dto.Brand,
                Model = dto.Model,
                ManufactureYear = dto.ManufactureYear,
                MachineryType = dto.MachineryType,
                FuelType = dto.FuelType,
                LoadCapacityKg = dto.LoadCapacityKg,
                MaxReachHeightMeters = dto.MaxReachHeightMeters,
                HourMeter = dto.HourMeter,
                Status = dto.Status,
                Notes = dto.Notes,
                PurchaseDate = dto.PurchaseDate
            };
        }
    }
}

