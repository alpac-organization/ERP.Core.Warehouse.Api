using AutoMapper;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class InventoryReportsProfile : Profile
    {
        public InventoryReportsProfile()
        {
            // Stock Diario
            CreateMap<ProductStock, StockDailyProductDto>()
                .ForMember(d => d.ProductCode, o => o.MapFrom(s => s.Product.Code))
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.ProductName))
                .ForMember(d => d.ProductDescription, o => o.MapFrom(s => s.Product.Description))
                .ForMember(d => d.InventoryStock, o => o.MapFrom(s => s.AvailableQuantity))
                .ForMember(d => d.UnitMeasure, o => o.MapFrom(s => s.UnitMeasure.Symbol))
                .ForMember(d => d.MinimumStock, o => o.MapFrom(s => s.MinimumStock))
                .ForMember(d => d.MaximumStock, o => o.MapFrom(s => s.MaximumStock));

            // Levantamiento Fisico
            CreateMap<ProductStock, PhysicalCountReportItemDto>()
                .ForMember(d => d.ProductCode, o => o.MapFrom(s => s.Product.Code))
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.ProductName))
                .ForMember(d => d.ProductDescription, o => o.MapFrom(s => s.Product.Description))
                .ForMember(d => d.BranchName, o => o.MapFrom(s => s.Product.Branch != null ? s.Product.Branch.BranchName : null))
                .ForMember(d => d.InventoryStock, o => o.MapFrom(s => s.AvailableQuantity))
                .ForMember(d => d.UnitMeasure, o => o.MapFrom(s => s.UnitMeasure != null ? s.UnitMeasure.Symbol : null))
                .ForMember(d => d.PhysicalStock, o => o.Ignore())
                .ForMember(d => d.Difference, o => o.Ignore())
                .ForMember(d => d.Observations, o => o.Ignore());
        }
    }
}