using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class WarehousePositionsProfile : Profile
{
    public WarehousePositionsProfile()
    {
        CreateMap<Sections, GetPositionsDto>()
            .ForMember(dest => dest.SectionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SectionCode, opt => opt.MapFrom(src => src.Code ?? string.Empty))
            .ForMember(dest => dest.Blocks, opt => opt.MapFrom((src, dest, context, resolutionContext) =>
                src.SectionStorageType == SectionStorageType.Lots
                    ? resolutionContext.Mapper.Map<List<PositionBlockDto>>(src.Lots.OrderBy(lot => lot.Code))
                    : resolutionContext.Mapper.Map<List<PositionBlockDto>>(src.Racks
                        .OrderBy(rack => rack.RowNumber)
                        .ThenBy(rack => rack.Code))));

        CreateMap<Lots, PositionBlockDto>()
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code ?? string.Empty))
            .ForMember(dest => dest.Positions, opt => opt.MapFrom(src => (src.Positions ?? Enumerable.Empty<LotsPositions>())
                .OrderBy(position => position.Row)
                .ThenBy(position => position.Column)
                .ThenBy(position => position.Level)
                .ToList()));

        CreateMap<Racks, PositionBlockDto>()
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code ?? string.Empty))
            .ForMember(dest => dest.Positions, opt => opt.MapFrom(src => (src.Positions ?? Enumerable.Empty<RackPositions>())
                .OrderBy(position => position.Row)
                .ThenBy(position => position.Column)
                .ThenBy(position => position.Level)
                .ToList()));

        CreateMap<LotsPositions, PositionItemDto>()
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.PositionCode ?? string.Empty))
            .ForMember(dest => dest.Coordinates, opt => opt.MapFrom(src =>
                src.LotsPositionsCoordinates != null && src.LotsPositionsCoordinates.DeletedAt == null
                    ? src.LotsPositionsCoordinates
                    : null));

        CreateMap<RackPositions, PositionItemDto>()
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.PositionCode ?? string.Empty))
            .ForMember(dest => dest.Coordinates, opt => opt.MapFrom(src =>
                src.RacksPositionsCoordinates != null && src.RacksPositionsCoordinates.DeletedAt == null
                    ? src.RacksPositionsCoordinates
                    : null));

        CreateMap<LotsPositionsCoordinates, PositionCoordinatesDto>();

        CreateMap<RacksPositionsCoordinates, PositionCoordinatesDto>();
    }
}
