using AutoMapper;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class KardexProfile : Profile
    {
        public KardexProfile()
        {
            CreateMap<Kardex, KardexDto>()
                .ForMember(dest => dest.KardexId,opt => opt.MapFrom(src => src.Id));
        }
    }
}