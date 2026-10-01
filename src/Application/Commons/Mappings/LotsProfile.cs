using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class LotsProfile : Profile
{
    public LotsProfile()
    {
        CreateMap<Lots, LotListItemDto>();

        CreateMap<LotsCapacity, LotCapacitiesDto>();

        CreateMap<LotsCoordinates, LotCoordinatesDto>();

        // Layout de tramos por seccion
        CreateMap<Lots, LotLayoutItemDto>()
            .ForMember(dest => dest.LotId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Width, opt => opt.MapFrom(src => src.LotsCapacity != null ? src.LotsCapacity.Width : 0m))
            .ForMember(dest => dest.Length, opt => opt.MapFrom(src => src.LotsCapacity != null ? src.LotsCapacity.Length : 0m))
            .ForMember(dest => dest.HasCoordinates, opt => opt.MapFrom(src => src.LotsCoordinates != null))
            .ForMember(dest => dest.PositionX, opt => opt.MapFrom(src => src.LotsCoordinates != null ? src.LotsCoordinates.PositionX : 0m))
            .ForMember(dest => dest.PositionY, opt => opt.MapFrom(src => src.LotsCoordinates != null ? src.LotsCoordinates.PositionY : 0m))
            .ForMember(dest => dest.PositionZ, opt => opt.MapFrom(src => src.LotsCoordinates != null ? src.LotsCoordinates.PositionZ : 0m))
            .ForMember(dest => dest.RotationY, opt => opt.MapFrom(src => src.LotsCoordinates != null ? src.LotsCoordinates.RotationY : 0m));

        CreateMap<Sections, LotLayoutDto>()
            .ForMember(dest => dest.SectionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SectionCode, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.SectionIsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.SectionWidth, opt => opt.MapFrom(src => src.SectionCapacity != null ? src.SectionCapacity.Width : 0m))
            .ForMember(dest => dest.SectionLength, opt => opt.MapFrom(src => src.SectionCapacity != null ? src.SectionCapacity.Length : 0m))
            .ForMember(dest => dest.SectionPositionX, opt => opt.MapFrom(src => src.SectionCoordinates != null ? src.SectionCoordinates.PositionX : 0m))
            .ForMember(dest => dest.SectionPositionY, opt => opt.MapFrom(src => src.SectionCoordinates != null ? src.SectionCoordinates.PositionY : 0m))
            .ForMember(dest => dest.Lots, opt => opt.Ignore());

        // Actualizacion desde Lots (Patch)
        CreateMap<LotsCapacity, LotsCapacity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LotsId, opt => opt.Ignore())
            .ForMember(dest => dest.Lot, opt => opt.Ignore());
    }

    public static Lots ToLotsEntity(RegisterLotsCommand request, string code)
    {
        return new Lots
        {
            Id             = Guid.NewGuid(),
            Code           = code,
            SectionId      = request.SectionId,
            NominalRows    = request.NominalRows,
            NominalColumns = request.NominalColumns,
            Status         = RackStatus.Available
        };
    }

    public static LotsPositions ToLotsPositionEntity(Guid lotId, string positionCode, int row, int column)
    {
        return new LotsPositions
        {
            LotId        = lotId,
            PositionCode = positionCode,
            Row          = row,
            Column       = column,
            Level        = 1
        };
    }

    public static LotsCoordinates ToLotCoordinatesEntity(CreateLotCoordinatesCommand request, Guid lotId)
    {
        return new LotsCoordinates
        {
            Id         = Guid.NewGuid(),
            LotId      = lotId,
            PositionX  = request.PositionX ?? 0,
            PositionY  = request.PositionY ?? 0,
            PositionZ  = request.PositionZ ?? 0,
            RotationY  = request.RotationY ?? 0
        };
    }
}