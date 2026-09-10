using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class LotsProfile : Profile
{
    public LotsProfile()
    {
        CreateMap<RegisterLotCommand, Lots>();

        CreateMap<Lots, LotListItemDto>()
            .ForMember(dest => dest.LotId, opt => opt.MapFrom(src => src.Id));

        CreateMap<LotsCapacity, LotCapacitiesDto>();
    }
}