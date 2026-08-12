using AutoMapper;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{

    public class PurchaseRequestItemsProfile : Profile
    {
        public PurchaseRequestItemsProfile()
        {    
            CreateMap<PurchaseRequestItem, PurchaseRequestItemDto>()
                .ForMember(dest => dest.PurchaseRequestItemId, opt => opt.MapFrom(src => src.Id))
                .ForPath(dest => dest.ProductDetails,   opt => opt.MapFrom(src => src.Product))
                .ForPath(dest => dest.UnitMeasureInformation,   opt => opt.MapFrom(src => src.UnitMeasure))
                .ForPath(dest => dest.ProductDetails.CategoryInformation,   opt => opt.MapFrom(src => src.Product.Category))
                
                //Cotizaciones de este item cotizado.
                .ForPath(dest => dest.Quotations,   opt => opt.MapFrom(src => src.Quotations));
        }
    }
}