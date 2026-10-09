using AutoMapper;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class ProductsProfile : Profile
    {
        public ProductsProfile()
        {
            CreateMap<Product, ProductDetails>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SupplierProducts, opt => opt.MapFrom(src => src.SupplierProducts));

            CreateMap<SupplierProduct, SupplierProductLinkDto>()
                .ForMember(dest => dest.SupplierProductId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SuppliersLegalName, opt => opt.MapFrom(src => src.Supplier.SuppliersLegalName))
                .ForMember(dest => dest.CommercialName, opt => opt.MapFrom(src => src.Supplier.CommercialName))
                .ForMember(dest => dest.TierPrices, opt => opt.MapFrom(src =>
                    src.TierPrices.Where(t => t.DeletedAt == null)));

            CreateMap<SupplierProductTierPrice, SupplierProductTierPriceDto>()
                .ForMember(dest => dest.TierPriceId, opt => opt.MapFrom(src => src.Id));
        }
    }
}
