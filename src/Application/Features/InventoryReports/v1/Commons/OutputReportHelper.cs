using KardexEntity = ERP.Core.Database.Domain.Entities.Inventory.Kardex;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

public static class OutputReportHelper
{
    public static List<OutputReportSource> BuildSources(
        List<KardexEntity> movements,
        List<Guid>? categoryIds,
        List<Guid>? productIds)
    {
        var sources = new List<OutputReportSource>();

        foreach (var kardex in movements)
        {
            var output = kardex.InventoryOutput;
            if (output == null)
                continue;

            var isFuel = output.FuelRequest != null;
            var hasEquipment = isFuel
                ? output.FuelRequest?.Equipment != null
                : output.SupplyRequest?.Equipment != null;

            if (!hasEquipment)
                continue;

            if (!MatchesFilters(kardex.Product?.CategoryId, kardex.ProductId, categoryIds, productIds))
                continue;

            var item = isFuel
                ? null
                : output.SupplyRequest?.SupplyRequestItems
                    .FirstOrDefault(i => i.ProductStock != null && i.ProductStock.ProductId == kardex.ProductId);

            sources.Add(new OutputReportSource
            {
                Output = output,
                Item = item,
                IsFuel = isFuel,
                Kardex = kardex
            });
        }

        return sources;
    }

    public static bool MatchesFilters(Guid? categoryId, Guid? productId, List<Guid>? categoryIds, List<Guid>? productIds)
    {
        if (categoryIds is { Count: > 0 } && !categoryIds.Contains(categoryId ?? Guid.Empty))
            return false;

        if (productIds is { Count: > 0 } && !productIds.Contains(productId ?? Guid.Empty))
            return false;

        return true;
    }

    public static string? GetOutputCode(OutputReportSource source) => source.Output.Code;

    public static string? GetRequestCode(OutputReportSource source) => source.IsFuel
        ? source.Output.FuelRequest?.Code
        : source.Output.SupplyRequest?.Code;

    public static DateTime GetOutputDate(OutputReportSource source) => source.Output.DispatchedAt ?? source.Output.CreatedAt;

    public static Guid GetCategoryId(OutputReportSource source) => source.Kardex?.Product?.CategoryId ?? Guid.Empty;

    public static string? GetCategoryName(OutputReportSource source) => source.Kardex?.Product?.Category?.Name;

    public static string GetCategoryNameOrDefault(OutputReportSource source) => GetCategoryName(source) ?? "Sin categoria";

    public static Guid GetEquipmentId(OutputReportSource source) => source.IsFuel
        ? source.Output.FuelRequest?.EquipmentId ?? Guid.Empty
        : source.Output.SupplyRequest?.EquipmentId ?? Guid.Empty;

    public static string GetEquipmentCode(OutputReportSource source) => source.IsFuel
        ? source.Output.FuelRequest?.Equipment?.Code ?? ""
        : source.Output.SupplyRequest?.Equipment?.Code ?? "";

    public static string? GetEquipmentDisplay(OutputReportSource source)
    {
        var equipment = source.IsFuel
            ? source.Output.FuelRequest?.Equipment
            : source.Output.SupplyRequest?.Equipment;

        return equipment != null ? (equipment.Brand + " " + equipment.Model).Trim() : null;
    }

    public static Guid GetProductId(OutputReportSource source) => source.Kardex?.ProductId ?? Guid.Empty;

    public static string? GetProductName(OutputReportSource source) => source.Kardex?.Product?.ProductName;

    public static string GetProductNameOrDefault(OutputReportSource source) => GetProductName(source) ?? "";

    public static string? GetStockCode(OutputReportSource source) => source.Kardex?.Product?.Code;

    public static decimal GetQuantity(OutputReportSource source) => source.Kardex?.Units ?? 0m;

    public static decimal GetUnitCost(OutputReportSource source) => source.Kardex?.UnitCost ?? 0m;

    public static decimal GetTotalCost(OutputReportSource source) => GetQuantity(source) * GetUnitCost(source);

    public static string? GetCostCenterName(OutputReportSource source) => source.Kardex?.CostCenter?.CostCenterName
        ?? (source.Output.CostCenter != null ? source.Output.CostCenter.CostCenterName : null);

    public static string? GetServiceOrderCode(OutputReportSource source) => !source.IsFuel
        && source.Output.SupplyRequest != null
        && source.Output.SupplyRequest.OperationalOrder != null
            ? source.Output.SupplyRequest.OperationalOrder.PoCode
            : null;

    public static string? GetCustomerName(OutputReportSource source) => source.IsFuel
        ? (source.Output.FuelRequest != null && source.Output.FuelRequest.Customer != null ? source.Output.FuelRequest.Customer.LegalName : null)
        : (source.Output.SupplyRequest != null && source.Output.SupplyRequest.Customer != null ? source.Output.SupplyRequest.Customer.LegalName : null);
}
