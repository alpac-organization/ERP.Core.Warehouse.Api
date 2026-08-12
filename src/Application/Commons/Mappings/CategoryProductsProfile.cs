using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class CategoryProductsProfile : Profile
    {
        public CategoryProductsProfile()
        {
            CreateMap<CategoryProducts, CategoryInformation>()
                .ForMember(dest => dest.CatagoryId, opt => opt.MapFrom(src => src.Id)); 
        }
    }
}