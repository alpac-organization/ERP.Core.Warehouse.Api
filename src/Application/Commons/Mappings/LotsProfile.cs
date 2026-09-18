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
}