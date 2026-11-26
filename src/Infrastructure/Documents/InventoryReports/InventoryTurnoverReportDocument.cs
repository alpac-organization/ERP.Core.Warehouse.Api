using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Infrastructure.Documents.InventoryReports;

public sealed class InventoryTurnoverReportDocument(
    InventoryTurnoverReportPdfModel model,
    CompanyBranding branding) : IDocument
{
    private readonly string _headerBg = branding.PrimaryColor;
    private readonly string _totalsBg = branding.HeaderColor;
    private const string BorderHex = "#CBD5E1";
    private const string AltRowBg = "#F8FAFC";
    private const string TextDark = "#0F172A";

    private static readonly CultureInfo InvariantFmt = CultureInfo.InvariantCulture;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.MarginHorizontal(7, Unit.Millimetre);
            page.MarginVertical(9, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(6.2f).FontColor(TextDark));

            page.Header().PaddingBottom(6).Row(row =>
            {
                if (branding.Logo is { Length: > 0 })
                {
                    row.ConstantItem(90).MaxHeight(45).AlignMiddle().Image(branding.Logo).FitArea();
                }

                row.RelativeItem().Column(header =>
                {
                    header.Spacing(2);
                    header.Item().AlignCenter().Text(model.CompanyName).Bold().FontSize(13);
                    header.Item().AlignCenter().Text("Reporte de Movimientos por rotación de Inventario").Bold().FontSize(11);
                    header.Item().PaddingTop(2).AlignCenter()
                        .Text($"Del {model.From:dd/MM/yyyy} Al {model.To:dd/MM/yyyy}")
                        .FontSize(8.5f).FontColor("#334155");
                });
            });

            page.Content().Element(ComposeTable);

            page.Footer().PaddingTop(5).BorderTop(0.5f).BorderColor(BorderHex).Row(row =>
            {
                var genBy = string.IsNullOrWhiteSpace(model.GeneratedBy) ? "Sistema" : model.GeneratedBy;
                row.RelativeItem().Text($"Generado por: {genBy}  |  Fecha: {model.GeneratedAt:dd/MM/yyyy HH:mm}")
                    .FontSize(6.8f).FontColor("#64748B");

                row.ConstantItem(120).AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(s => s.FontSize(6.8f).FontColor("#64748B"));
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1.38f); // 1. Línea
                cols.RelativeColumn(1.58f); // 2. Producto
                cols.RelativeColumn(1.10f); // 3. SKU
                cols.RelativeColumn(0.85f); // 4. Unidades Disponible
                cols.RelativeColumn(0.95f); // 5. Costo Unitario
                cols.RelativeColumn(1.05f); // 6. Total
                cols.RelativeColumn(0.80f); // 7. Compras totales
                cols.RelativeColumn(0.95f); // 8. Fecha de última compra
                cols.RelativeColumn(0.85f); // 9. Despachos Totales
                cols.RelativeColumn(0.85f); // 10. Unidades Compras
                cols.RelativeColumn(0.95f); // 11. Fecha último despacho
                cols.RelativeColumn(1.08f); // 12. Unidades Despachadas
                cols.RelativeColumn(0.95f); // 13. Costo histórico mín
                cols.RelativeColumn(0.95f); // 14. Costo histórico máx
                cols.RelativeColumn(0.80f); // 15. Mínimo de Unidades
                cols.RelativeColumn(0.80f); // 16. Máximo de Unidades
                cols.RelativeColumn(0.80f); // 17. Stock Mínimo
                cols.RelativeColumn(0.80f); // 18. Stock Máximo
                cols.RelativeColumn(0.90f); // 19. Índice de Rotación Mensual
                cols.RelativeColumn(0.90f); // 20. Índice de Rotación Anual
            });

            table.Header(header =>
            {
                string[] headers =
                [
                    "Línea",
                    "Producto",
                    "SKU",
                    "Unidades Disponible",
                    "Costo Unitario",
                    "Total",
                    "Compras totales",
                    "Fecha última compra",
                    "Despachos Totales",
                    "Unidades Compras",
                    "Fecha último despacho",
                    "Unidades Despachadas",
                    "Costo histórico mín",
                    "Costo histórico máx",
                    "Mínimo de Unidades",
                    "Máximo de Unidades",
                    "Stock Mínimo",
                    "Stock Máximo",
                    "Índice Rotación Mensual",
                    "Índice Rotación Anual"
                ];

                for (var i = 0; i < headers.Length; i++)
                {
                    var alignRight = i >= 3 && i != 7 && i != 10;
                    var cell = header.Cell()
                        .Background(_headerBg)
                        .Border(0.5f).BorderColor(_headerBg)
                        .PaddingVertical(4).PaddingHorizontal(2.5f)
                        .AlignMiddle();

                    if (alignRight)
                        cell.AlignRight().Text(headers[i]).Bold().FontSize(6f).FontColor(Colors.White);
                    else
                        cell.AlignLeft().Text(headers[i]).Bold().FontSize(6f).FontColor(Colors.White);
                }
            });

            if (model.Items.Count == 0)
            {
                table.Cell().ColumnSpan(20)
                    .Border(0.5f).BorderColor(BorderHex)
                    .PaddingVertical(8).PaddingHorizontal(6)
                    .AlignCenter()
                    .Text("Sin registros de inventario para los filtros seleccionados.")
                    .Italic().FontSize(7.5f).FontColor("#64748B");
            }
            else
            {
                for (var idx = 0; idx < model.Items.Count; idx++)
                {
                    var item = model.Items[idx];
                    var bg = idx % 2 == 1 ? AltRowBg : "#FFFFFF";

                    AddCell(table, item.Category ?? "—", bg, alignRight: false);
                    AddCell(table, item.ProductName ?? "—", bg, alignRight: false);
                    AddCell(table, item.StockCode ?? "—", bg, alignRight: false);
                    AddCell(table, FormatUnits(item.AvailableUnits), bg, alignRight: true);
                    AddCell(table, FormatCurrency(item.UnitCost), bg, alignRight: true);
                    AddCell(table, FormatCurrency(item.Total), bg, alignRight: true);
                    AddCell(table, FormatUnits(item.TotalPurchases), bg, alignRight: true);
                    AddCell(table, FormatDate(item.LastPurchaseDate), bg, alignRight: false);
                    AddCell(table, FormatUnits(item.TotalDispatches), bg, alignRight: true);
                    AddCell(table, FormatUnits(item.PurchaseUnits), bg, alignRight: true);
                    AddCell(table, FormatDate(item.LastDispatchDate), bg, alignRight: false);
                    AddCell(table, FormatUnits(item.DispatchedUnits), bg, alignRight: true);
                    AddCell(table, FormatCurrency(item.HistoricalMinCost), bg, alignRight: true);
                    AddCell(table, FormatCurrency(item.HistoricalMaxCost), bg, alignRight: true);
                    AddCell(table, FormatUnits(item.MinDispatchedUnits), bg, alignRight: true);
                    AddCell(table, FormatUnits(item.MaxDispatchedUnits), bg, alignRight: true);
                    AddCell(table, FormatUnits(item.ConfiguredMinStock), bg, alignRight: true);
                    AddCell(table, item.ConfiguredMaxStock.HasValue ? FormatUnits(item.ConfiguredMaxStock.Value) : "—", bg, alignRight: true);
                    AddCell(table, FormatUnits(item.MonthlyRotationIndex), bg, alignRight: true);
                    AddCell(table, FormatUnits(item.AnnualRotationIndex), bg, alignRight: true);
                }
            }

            // Fila Totales (igual al Excel A7:C7 "Totales", D7, F7, G7, I7, J7, L7)
            var sumAvailableUnits = model.Items.Sum(x => x.AvailableUnits);
            var sumTotal = model.Items.Sum(x => x.Total);
            var sumTotalPurchases = model.Items.Sum(x => x.TotalPurchases);
            var sumTotalDispatches = model.Items.Sum(x => x.TotalDispatches);
            var sumPurchaseUnits = model.Items.Sum(x => x.PurchaseUnits);
            var sumDispatchedUnits = model.Items.Sum(x => x.DispatchedUnits);

            table.Cell().ColumnSpan(3)
                .Background(_totalsBg)
                .Border(0.5f).BorderColor(BorderHex)
                .PaddingVertical(4).PaddingHorizontal(4)
                .AlignLeft().AlignMiddle()
                .Text("Totales").Bold().FontSize(6.5f);

            AddTotalsCell(table, FormatUnits(sumAvailableUnits));
            AddTotalsCell(table, "");
            AddTotalsCell(table, FormatCurrency(sumTotal));
            AddTotalsCell(table, FormatUnits(sumTotalPurchases));
            AddTotalsCell(table, "");
            AddTotalsCell(table, FormatUnits(sumTotalDispatches));
            AddTotalsCell(table, FormatUnits(sumPurchaseUnits));
            AddTotalsCell(table, "");
            AddTotalsCell(table, FormatUnits(sumDispatchedUnits));

            table.Cell().ColumnSpan(8)
                .Background(_totalsBg)
                .Border(0.5f).BorderColor(BorderHex)
                .PaddingVertical(4).PaddingHorizontal(2.5f)
                .Text("");
        });
    }

    private static void AddCell(TableDescriptor table, string value, string bg, bool alignRight)
    {
        var cell = table.Cell()
            .Background(bg)
            .Border(0.5f).BorderColor(BorderHex)
            .PaddingVertical(3).PaddingHorizontal(2.5f)
            .AlignMiddle();

        if (alignRight)
            cell.AlignRight().Text(value);
        else
            cell.AlignLeft().Text(value);
    }

    private void AddTotalsCell(TableDescriptor table, string value)
    {
        table.Cell()
            .Background(_totalsBg)
            .Border(0.5f).BorderColor(BorderHex)
            .PaddingVertical(4).PaddingHorizontal(2.5f)
            .AlignRight().AlignMiddle()
            .Text(value).Bold().FontSize(6.4f);
    }

    private static string FormatDate(DateTime? date) =>
        date.HasValue ? date.Value.ToString("dd/MM/yyyy", InvariantFmt) : "—";

    private static string FormatCurrency(decimal value) => value.ToString("#,##0.00", InvariantFmt);

    private static string FormatUnits(decimal value) =>
        value == Math.Truncate(value)
            ? value.ToString("#,##0", InvariantFmt)
            : value.ToString("#,##0.##", InvariantFmt);
}
