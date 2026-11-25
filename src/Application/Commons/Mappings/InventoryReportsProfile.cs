using AutoMapper;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;
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

            // Inventario por Periodo
            CreateMap<ProductStock, PeriodInventoryItemDto>()
                .ForMember(d => d.StockCode, o => o.MapFrom(s => s.Product.Code))
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.ProductName));

            // Salidas por Linea-Equipo
            CreateMap<OutputReportSource, OutputLineEquipmentItemDto>()
                .ForMember(d => d.OutputCode, o => o.MapFrom(s => OutputReportHelper.GetOutputCode(s)))
                .ForMember(d => d.RequestCode, o => o.MapFrom(s => OutputReportHelper.GetRequestCode(s)))
                .ForMember(d => d.OutputDate, o => o.MapFrom(s => OutputReportHelper.GetOutputDate(s)))
                .ForMember(d => d.Quantity, o => o.MapFrom(s => OutputReportHelper.GetQuantity(s)))
                .ForMember(d => d.Cost, o => o.MapFrom(s => OutputReportHelper.GetUnitCost(s)))
                .ForMember(d => d.TotalCost, o => o.MapFrom(s => OutputReportHelper.GetTotalCost(s)))
                .ForMember(d => d.CostCenter, o => o.MapFrom(s => OutputReportHelper.GetCostCenterName(s)))
                .ForMember(d => d.ServiceOrderCode, o => o.MapFrom(s => OutputReportHelper.GetServiceOrderCode(s)))
                .ForMember(d => d.Customer, o => o.MapFrom(s => OutputReportHelper.GetCustomerName(s)));

            // Salidas a Detalle por Equipo
            CreateMap<OutputReportSource, OutputDetailByEquipmentItemDto>()
                .ForMember(d => d.OutputCode, o => o.MapFrom(s => OutputReportHelper.GetOutputCode(s)))
                .ForMember(d => d.RequestCode, o => o.MapFrom(s => OutputReportHelper.GetRequestCode(s)))
                .ForMember(d => d.Equipment, o => o.MapFrom(s => OutputReportHelper.GetEquipmentDisplay(s)))
                .ForMember(d => d.Category, o => o.MapFrom(s => OutputReportHelper.GetCategoryName(s)))
                .ForMember(d => d.OutputDate, o => o.MapFrom(s => OutputReportHelper.GetOutputDate(s)))
                .ForMember(d => d.StockCode, o => o.MapFrom(s => OutputReportHelper.GetStockCode(s)))
                .ForMember(d => d.Supply, o => o.MapFrom(s => OutputReportHelper.GetProductName(s)))
                .ForMember(d => d.Quantity, o => o.MapFrom(s => OutputReportHelper.GetQuantity(s)))
                .ForMember(d => d.Cost, o => o.MapFrom(s => OutputReportHelper.GetUnitCost(s)))
                .ForMember(d => d.TotalCost, o => o.MapFrom(s => OutputReportHelper.GetTotalCost(s)))
                .ForMember(d => d.CostCenter, o => o.MapFrom(s => OutputReportHelper.GetCostCenterName(s)))
                .ForMember(d => d.ServiceOrderCode, o => o.MapFrom(s => OutputReportHelper.GetServiceOrderCode(s)))
                .ForMember(d => d.Customer, o => o.MapFrom(s => OutputReportHelper.GetCustomerName(s)));

            // Salidas Consolidado por Equipo
            CreateMap<OutputReportSource, OutputConsolidatedItemDto>()
                .ForMember(d => d.Equipment, o => o.MapFrom(s => OutputReportHelper.GetEquipmentDisplay(s)))
                .ForMember(d => d.StockCode, o => o.MapFrom(s => OutputReportHelper.GetStockCode(s)))
                .ForMember(d => d.Supply, o => o.MapFrom(s => OutputReportHelper.GetProductName(s)))
                .ForMember(d => d.Quantity, o => o.MapFrom(s => OutputReportHelper.GetQuantity(s)))
                .ForMember(d => d.TotalCost, o => o.MapFrom(s => OutputReportHelper.GetTotalCost(s)))
                .ForMember(d => d.CostCenter, o => o.MapFrom(s => OutputReportHelper.GetCostCenterName(s)))
                .ForMember(d => d.Customer, o => o.MapFrom(s => OutputReportHelper.GetCustomerName(s)));
        }
    }
}
