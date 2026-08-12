using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
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
        .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.WarehouseName))
        .ForMember(dest => dest.WarehouseType, opt => opt.MapFrom(src => src.WarehouseType.ToString()))
        .ForMember(dest => dest.SubWarehouses, opt => opt.MapFrom(src => src.SubWarehouses));

        #region Sections
        CreateMap<Sections, SectionDto>()
            .ForMember(dest => dest.SectionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SectionCode, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.SectionName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.SectionType, opt => opt.MapFrom(src => src.SectionType.ToString()));
        #endregion
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
            WarehouseId = warehouseId,
            WitdhMetres = command.WarehouseDetails.WidthMetres,
            LengthMetres = command.WarehouseDetails.LengthMetres,
            RampsCount = command.WarehouseDetails.RampsCount,
            ParkingSpacesCount = command.WarehouseDetails.ParkingSpacesCount
        };

        return new()
        {
            Id = warehouseId,
            Code = command.Code,
            IsActive = true,
            WarehouseName = command.WarehouseName,
            BranchId = command.BranchId,
            WarehouseType = command.WarehouseType,
            ParentWarehouseId = command.ParentWarehouseId,

            Details = details,
            Capacity = details.ToCalculatedCapacity(warehouseId)
        };
    }

    public static WarehouseCapacity ToCalculatedCapacity(this WarehouseDetails details, Guid warehouseId)
    {
        var totalArea = details.WitdhMetres * details.LengthMetres;

        return new WarehouseCapacity
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            TotalAreaM2 = totalArea,
            UsableAreaM2 = null, // aún no hay secciones/racks para descontar
            UnusableAreaM2 = null,
            TotalMaxPolines = null,
            CurrentPolinesStored = null,
            LastCalculatedAt = NicaraguaClock.Now
        };
    }
}

#region Sections
public static class SectionMapper
{
    public static Sections ToSectionEntity(this Commands.RegisterSectionCommand command, string enabledByUserName)
    {
        var sectionId = Guid.NewGuid();

        SectionOverflowCapacity? overflowCapacity = null;

        if (command.SectionType == SectionType.Aisle && command.OverflowCapacity is not null)
        {
            var nowNica = NicaraguaClock.Now;

            overflowCapacity = new SectionOverflowCapacity
            {
                Id = Guid.NewGuid(),
                SectionId = sectionId,
                AllowsOverflowStorage = command.OverflowCapacity.AllowsOverflowStorage,
                IsOverflowEnabled = command.OverflowCapacity.IsOverflowEnabled,
                MaxOverflowPolines = command.OverflowCapacity.MaxOverflowPolines,
                EnabledByUserName = enabledByUserName,
                EnabledDate = DateOnly.FromDateTime(nowNica),
                EnabledTime = TimeOnly.FromDateTime(nowNica)
            };
        }

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

            OverflowCapacity = overflowCapacity
        };
    }
}
#endregion