using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using Commands = ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

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
            IsActive = true,
            Id = Guid.NewGuid(),
            WarehouseName = command.WarehouseName,
            BranchId = command.BranchId,
            WarehouseType = command.WarehouseInformation.WarehouseType,
        };
    }

    public static Sections ToSectionEntity(this Commands.SectionInformation command, Guid warehouseId, string sectionCode)
    {
        return new()
        {
            IsActive = true,
            Code = sectionCode,
            Id = Guid.NewGuid(),
            Name = command.ZoneName,
            WarehouseId = warehouseId,
            LengthMetres = command.LengthMetres,
        };
    }

    public static Racks ToRackEntity(this Commands.RackInformation command, Guid zoneId)
    {
        return new()
        {
            SectionId = zoneId,
            RowNumber = command.RowNumber,
            LevelNumber = command.LevelNumber,
        };
    }
}

#region Sections
public static class SectionMapper
{
    public static Sections ToSectionEntity(this Commands.RegisterSectionCommand command)
    {
        var sectionId = Guid.NewGuid();

        return new()
        {
            Id = sectionId,
            IsActive = true,
            Code = command.Code,
            Name = command.Name,
            SectionType = command.SectionType,
            WidthMetres = command.WidthMetres,
            LengthMetres = command.LengthMetres,
            WarehouseId = command.WarehouseId,

            OverflowCapacity = command.OverflowCapacity is null
                ? null
                : new SectionOverflowCapacity
                {
                    Id = Guid.NewGuid(),
                    SectionId = sectionId,
                    AllowsOverflowStorage = command.OverflowCapacity.AllowsOverflowStorage,
                    IsOverflowEnabled = command.OverflowCapacity.IsOverflowEnabled,
                    MaxOverflowPolines = command.OverflowCapacity.MaxOverflowPolines,
                    EnabledByUserName = command.OverflowCapacity.EnabledByUserName,
                    EnabledDate = command.OverflowCapacity.EnabledDate,
                    EnabledTime = command.OverflowCapacity.EnabledTime
                }
        };
    }
}
#endregion