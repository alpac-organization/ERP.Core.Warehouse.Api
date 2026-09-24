using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class RackProfile : Profile
{
    public RackProfile()
    {
        CreateMap<Racks, RackListDto>()
            .ForMember(dest => dest.RackId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Width, opt => opt.MapFrom(src => src.RackCapacity != null ? src.RackCapacity.Width : 0m))
            .ForMember(dest => dest.Length, opt => opt.MapFrom(src => src.RackCapacity != null ? src.RackCapacity.Length : 0m))
            .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.RackCapacity != null ? src.RackCapacity.Height : null))
            .ForMember(dest => dest.PositionX, opt => opt.MapFrom(src => src.RacksCoordinates != null ? src.RacksCoordinates.PositionX : 0m))
            .ForMember(dest => dest.PositionY, opt => opt.MapFrom(src => src.RacksCoordinates != null ? src.RacksCoordinates.PositionY : 0m))
            .ForMember(dest => dest.RotationY, opt => opt.MapFrom(src => src.RacksCoordinates != null ? src.RacksCoordinates.RotationY : 0m))
            .ForMember(dest => dest.TotalPositions, opt => opt.MapFrom(src => src.Positions.Count))
            .ForMember(dest => dest.OccupiedPositions, opt => opt.MapFrom(src => src.Positions.Count(p => p.Status == RackStatus.Occupied)))
            .ForMember(dest => dest.AvailablePositions, opt => opt.MapFrom(src => src.Positions.Count(p => p.Status == RackStatus.Available)));

        CreateMap<RackCapacity, RackCapacityDto>()
            .ForMember(dest => dest.RackCapacityId, opt => opt.MapFrom(src => src.Id));

        CreateMap<RacksCoordinates, RackCoordinatesDto>()
            .ForMember(dest => dest.RackCoordinateId, opt => opt.MapFrom(src => src.Id));

        CreateMap<RackPositions, RackPositionDetailDto>()
            .ForMember(dest => dest.PositionId, opt => opt.MapFrom(src => src.Id));

        CreateMap<Racks, RackDetailsDto>()
            .ForMember(dest => dest.RackId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Capacity, opt => opt.MapFrom(src => src.RackCapacity))
            .ForMember(dest => dest.Coordinates, opt => opt.MapFrom(src => src.RacksCoordinates))
            .ForMember(dest => dest.Positions, opt => opt.MapFrom(src => src.Positions));
    }

    public static Racks ToRackEntity(Guid sectionId, string code, int rowNumber, int levelNumber, int maxPulleys, RackUsageProfile usageProfile)
    {
        return new Racks
        {
            Id = Guid.NewGuid(),
            SectionId = sectionId,
            Code = code,
            RowNumber = rowNumber,
            LevelNumber = levelNumber,
            MaxPulleys = maxPulleys,
            UsageProfile = usageProfile,
            Status = RackStatus.Available
        };
    }

    public static RackPositions ToRackPositionEntity(Guid rackId, string positionCode, int row, int column, int level)
    {
        return new RackPositions
        {
            Id = Guid.NewGuid(),
            RackId = rackId,
            PositionCode = positionCode,
            Row = row,
            Column = column,
            Level = level,
            Status = RackStatus.Available,
            AllowsStocking = true
        };
    }

    public static RacksCoordinates ToRackCoordinatesEntity(Guid rackId, decimal posX, decimal posY, decimal posZ = 0m, decimal rotY = 0m)
    {
        return new RacksCoordinates
        {
            Id = Guid.NewGuid(),
            RackId = rackId,
            PositionX = posX,
            PositionY = posY,
            PositionZ = posZ,
            RotationY = rotY
        };
    }
}
