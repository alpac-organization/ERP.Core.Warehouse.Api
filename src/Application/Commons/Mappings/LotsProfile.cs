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
        CreateMap<RegisterLotsCommand, Lots>();

        CreateMap<Lots, LotListItemDto>();

        CreateMap<LotsCapacity, LotCapacitiesDto>();

        // Actualizacion desde Lots (Patch)
        CreateMap<LotsCapacity, LotsCapacity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LotsId, opt => opt.Ignore())
            .ForMember(dest => dest.Lot, opt => opt.Ignore());
    }
}

public static class LotsMapper
{
    public static Lots ToLotsEntity(IMapper mapper, RegisterLotsCommand command, string code, Guid sectionId)
    {
        var lot = mapper.Map<Lots>(command);
        lot.Id = Guid.NewGuid();
        lot.Code = code;
        lot.SectionId = sectionId;
        lot.Status = RackStatus.Available;
        return lot;
    }

    public static LotsPositions ToLotsPositionEntity(Guid lotId, string positionCode, int row, int column)
    {
        return new()
        {
            Id             = Guid.NewGuid(),
            LotId          = lotId,
            PositionCode   = positionCode,
            Row            = row,
            Column         = column,
            Level          = 1,
            AllowsStocking = true
        };
    }
}