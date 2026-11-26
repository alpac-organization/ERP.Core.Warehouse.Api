using ERP.Core.Database.Domain.Entities.Inventory;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

public static class InventoryMovementHelper
{
    public static DateTime GetDate(Kardex kardex) =>
        kardex.TransactionDate.ToDateTime(kardex.TransactionTime);

    public static string? GetStockCode(Kardex kardex) => kardex.Product?.Code;

    public static string? GetProductName(Kardex kardex) => kardex.Product?.ProductName;

    public static decimal GetUnits(Kardex kardex) => kardex.Units;

    public static decimal GetCost(Kardex kardex) => kardex.UnitCost;

    public static decimal GetEntryTotal(Kardex kardex) => kardex.TotalInput;

    public static decimal GetExitTotal(Kardex kardex) => kardex.TotalOutput;

    public static string? GetEntryDocumentCode(InventoryMovementEntrySource source) => source.Entry?.Code;

    public static string? GetEntryReference(InventoryMovementEntrySource source)
    {
        var invoiceNumber = source.Entry?.InvoiceNumber;
        if (!string.IsNullOrWhiteSpace(invoiceNumber))
        {
            var reference = invoiceNumber.Trim();
            return reference.StartsWith("FT#", StringComparison.OrdinalIgnoreCase)
                ? reference
                : $"FT#{reference}";
        }

        return source.Entry?.PurchaseOrder?.Code;
    }

    public static string? GetSupplier(InventoryMovementEntrySource source) =>
        source.Entry?.PurchaseOrder?.Supplier?.SuppliersLegalName
        ?? source.Entry?.PurchaseOrder?.Supplier?.CommercialName
        ?? source.AdditionalItem?.SupplierName
        ?? source.RootSupplierName;

    public static string? GetExitReference(OutputReportSource source)
    {
        var rawCode = OutputReportHelper.GetRequestCode(source);
        if (string.IsNullOrWhiteSpace(rawCode))
            return null;

        var trimmed = rawCode.Trim();
        var prefix = source.IsFuel ? "BL#" : "RQ#";
        return trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{prefix}{trimmed}";
    }
}
