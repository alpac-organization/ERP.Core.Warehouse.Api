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