using AutoMapper;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class InventoryStockProfile : Profile
    {
        public InventoryStockProfile()
        {
            CreateMap<ProductStock, InventoryStockDto>()
                .ForMember(dest => dest.ProductStockId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ProductInformation, opt => opt.MapFrom(src => src.Product))
                .ForMember(dest => dest.CostCenterInformation, opt => opt.MapFrom(src => src.CostCenter))
                .ForMember(dest => dest.UnitMeasureInformation, opt => opt.MapFrom(src => src.UnitMeasure));

            CreateMap<Product, ProductInformation>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.CategoryInformation, opt => opt.MapFrom(src => src.Category));

            CreateMap<CategoryProducts, CategoryInformation>()
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.Id));
        
            CreateMap<UnitMeasure, UnitMeasureInformation>();
        }
    }
}