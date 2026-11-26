using AutoMapper;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commands;

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

            // Levantamiento Fisico - POST
            CreateMap<CreatePhysicalCountCommand, PhysicalCount>()
                .ForMember(d => d.Id, o => o.MapFrom(s => Guid.NewGuid()))
                .ForMember(d => d.CompanyId, o => o.MapFrom(s => s.CompanyId))
                .ForMember(d => d.BranchId, o => o.MapFrom(s => s.BranchId))
                .ForMember(d => d.Status, o => o.MapFrom(s => PhysicalCountStatus.Draft))
                .ForMember(d => d.Notes, o => o.MapFrom(s => s.Notes))
                .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.UserId))
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.DeletedAt, o => o.Ignore())
                .ForMember(d => d.CompletedAt, o => o.Ignore())
                .ForMember(d => d.CompletedBy, o => o.Ignore())
                .ForMember(d => d.TotalLines, o => o.MapFrom(s => s.TotalLines))
                .ForMember(d => d.CompletedLines, o => o.MapFrom(s => 0))
                .ForMember(d => d.Company, o => o.Ignore())
                .ForMember(d => d.Branch, o => o.Ignore())
                .ForMember(d => d.Lines, o => o.Ignore());

            CreateMap<ProductStock, PhysicalCountLine>()
                .ForMember(d => d.Id, o => o.MapFrom(s => Guid.NewGuid()))
                .ForMember(d => d.PhysicalCountId, o => o.Ignore())
                .ForMember(d => d.ProductStockId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.SystemStock, o => o.MapFrom(s => s.AvailableQuantity))
                .ForMember(d => d.PhysicalStock, o => o.Ignore())
                .ForMember(d => d.Difference, o => o.Ignore())
                .ForMember(d => d.Observations, o => o.Ignore())
                .ForMember(d => d.CountedByUserId, o => o.Ignore())
                .ForMember(d => d.CountedAt, o => o.Ignore())
                .ForMember(d => d.UpdatedBy, o => o.Ignore())
                .ForMember(d => d.UpdatedAt, o => o.Ignore())
                .ForMember(d => d.PhysicalCount, o => o.Ignore())
                .ForMember(d => d.ProductStock, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.DeletedAt, o => o.Ignore());

            // Levantamiento Fisico - PATCH
            CreateMap<UpdatePhysicalCountLineSource, PhysicalCountLine>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.PhysicalCountId, o => o.Ignore())
                .ForMember(d => d.ProductStockId, o => o.Ignore())
                .ForMember(d => d.SystemStock, o => o.Ignore())
                .ForMember(d => d.PhysicalStock, o => o.MapFrom(s => s.PhysicalStock))
                .ForMember(d => d.Observations, o => o.MapFrom((s, d) => s.Observations ?? d.Observations))
                .ForMember(d => d.Difference, o => o.MapFrom((s, d) => s.PhysicalStock.HasValue
                    ? s.PhysicalStock.Value - d.SystemStock
                    : d.Difference))
                .ForMember(d => d.CountedByUserId, o => o.MapFrom((s, d) => s.PhysicalStock.HasValue ? s.UserId : d.CountedByUserId))
                .ForMember(d => d.CountedAt, o => o.MapFrom((s, d) => s.PhysicalStock.HasValue ? s.CountedAt : d.CountedAt))
                .ForMember(d => d.UpdatedBy, o => o.MapFrom(s => s.UserId))
                .ForMember(d => d.UpdatedAt, o => o.MapFrom(s => s.CountedAt))
                .ForMember(d => d.PhysicalCount, o => o.Ignore())
                .ForMember(d => d.ProductStock, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.DeletedAt, o => o.Ignore());

            CreateMap<PhysicalCount, PhysicalCountListItemDto>()
                .ForMember(d => d.PhysicalCountId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.CreatedByName, o => o.Ignore());

            CreateMap<PhysicalCount, PhysicalCountSummaryDto>()
                .ForMember(d => d.PhysicalCountId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.BranchName, o => o.MapFrom(s => s.Branch != null ? s.Branch.BranchName : null))
                .ForMember(d => d.CreatedByName, o => o.Ignore())
                .ForMember(d => d.CompletedByName, o => o.Ignore())
                .ForMember(d => d.PendingLines, o => o.Ignore())
                .ForMember(d => d.CanPrint, o => o.Ignore());

            CreateMap<PhysicalCountLine, PhysicalCountLineDto>()
                .ForMember(d => d.PhysicalCountLineId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.ProductCode, o => o.MapFrom(s => s.ProductStock.Product.Code))
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.ProductStock.Product.ProductName))
                .ForMember(d => d.UnitMeasure, o => o.MapFrom(s =>
                    s.ProductStock.UnitMeasure != null ? s.ProductStock.UnitMeasure.Symbol : null))
                .ForMember(d => d.CountedByName, o => o.Ignore());

            // Inventario por Periodo
            CreateMap<ProductStock, PeriodInventoryItemDto>()
                .ForMember(d => d.StockCode, o => o.MapFrom(s => s.Product.Code))
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.ProductName));

            // Rotacion de Inventario
            CreateMap<ProductStock, InventoryTurnoverReportDto>()
                .ForMember(d => d.Category, o => o.MapFrom(s => s.Product.Category != null ? s.Product.Category.Name : null))
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.ProductName))
                .ForMember(d => d.StockCode, o => o.MapFrom(s => s.Product.Code))
                .ForMember(d => d.AvailableUnits, o => o.MapFrom(s => s.AvailableQuantity))
                .ForMember(d => d.UnitCost, o => o.MapFrom(s => s.WeightedAverageCost))
                .ForMember(d => d.Total, o => o.MapFrom(s => s.AvailableQuantity * s.WeightedAverageCost))
                .ForMember(d => d.ConfiguredMinStock, o => o.MapFrom(s => s.MinimumStock))
                .ForMember(d => d.ConfiguredMaxStock, o => o.MapFrom(s => s.MaximumStock));

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
                .ForMember(d => d.Reference, o => o.MapFrom(s => InventoryMovementHelper.GetExitReference(s.OutputSource)))
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
