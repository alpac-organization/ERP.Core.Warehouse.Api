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

            // Movimiento de Inventario - Entradas
            CreateMap<InventoryMovementEntrySource, InventoryMovementEntryDto>()
                .ForMember(d => d.Date, o => o.MapFrom(s => InventoryMovementHelper.GetDate(s.Kardex)))
                .ForMember(d => d.DocumentCode, o => o.MapFrom(s => InventoryMovementHelper.GetEntryDocumentCode(s)))
                .ForMember(d => d.StockCode, o => o.MapFrom(s => InventoryMovementHelper.GetStockCode(s.Kardex)))
                .ForMember(d => d.ProductName, o => o.MapFrom(s => InventoryMovementHelper.GetProductName(s.Kardex)))
                .ForMember(d => d.Reference, o => o.MapFrom(s => InventoryMovementHelper.GetEntryReference(s)))
                .ForMember(d => d.Units, o => o.MapFrom(s => InventoryMovementHelper.GetUnits(s.Kardex)))
                .ForMember(d => d.Cost, o => o.MapFrom(s => InventoryMovementHelper.GetCost(s.Kardex)))
                .ForMember(d => d.Total, o => o.MapFrom(s => InventoryMovementHelper.GetEntryTotal(s.Kardex)))
                .ForMember(d => d.Supplier, o => o.MapFrom(s => InventoryMovementHelper.GetSupplier(s)))
                .ForMember(d => d.MinimumCost, o => o.MapFrom(s => s.MinimumCost))
                .ForMember(d => d.MaximumCost, o => o.MapFrom(s => s.MaximumCost));

            // Movimiento de Inventario - Salidas
            CreateMap<InventoryMovementExitSource, InventoryMovementExitDto>()
                .ForMember(d => d.Date, o => o.MapFrom(s => InventoryMovementHelper.GetDate(s.Kardex)))
                .ForMember(d => d.DocumentCode, o => o.MapFrom(s => OutputReportHelper.GetOutputCode(s.OutputSource)))
                .ForMember(d => d.StockCode, o => o.MapFrom(s => InventoryMovementHelper.GetStockCode(s.Kardex)))
                .ForMember(d => d.ProductName, o => o.MapFrom(s => InventoryMovementHelper.GetProductName(s.Kardex)))
                .ForMember(d => d.Reference, o => o.MapFrom(s => OutputReportHelper.GetRequestCode(s.OutputSource)))
                .ForMember(d => d.Units, o => o.MapFrom(s => InventoryMovementHelper.GetUnits(s.Kardex)))
                .ForMember(d => d.Cost, o => o.MapFrom(s => InventoryMovementHelper.GetCost(s.Kardex)))
                .ForMember(d => d.Total, o => o.MapFrom(s => InventoryMovementHelper.GetExitTotal(s.Kardex)))
                .ForMember(d => d.ServiceOrderCode, o => o.MapFrom(s => OutputReportHelper.GetServiceOrderCode(s.OutputSource)))
                .ForMember(d => d.MaximumCost, o => o.MapFrom(s => s.MaximumCost));

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
