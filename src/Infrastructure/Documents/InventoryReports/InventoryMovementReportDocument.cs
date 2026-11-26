using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Infrastructure.Documents.InventoryReports;

public sealed class InventoryMovementReportDocument(
    InventoryMovementReportDto report,
    string companyName) : IDocument
{
    private const string SectionBarBg = "#156082";
    private const string HeaderBg = "#CAEDFB";
    private const string SubtotalBg = "#C0E6F5";
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
            page.MarginHorizontal(10, Unit.Millimetre);
            page.MarginVertical(10, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(TextDark));

            page.Header().PaddingBottom(8).Column(header =>
            {
                header.Spacing(2);
                header.Item().AlignCenter().Text(companyName).Bold().FontSize(13);
                header.Item().AlignCenter().Text("Movimientos de Inventario").Bold().FontSize(11);
                header.Item().PaddingTop(2).AlignCenter()
                    .Text($"Del {report.From:dd/MM/yyyy} Al {report.To:dd/MM/yyyy}")
                    .FontSize(8.5f).FontColor("#334155");
            });

            page.Content().Column(col =>
            {
                col.Spacing(10);

                col.Item().Element(ComposeEntriesSection);
                col.Item().Element(ComposeExitsSection);
                col.Item().Element(ComposeProductMovementSummary);
            });

            page.Footer().PaddingTop(6).BorderTop(0.5f).BorderColor(BorderHex).Row(row =>
            {
                var genBy = string.IsNullOrWhiteSpace(report.GeneratedBy) ? "Sistema" : report.GeneratedBy;
                row.RelativeItem().Text($"Generado por: {genBy}  |  Fecha: {report.GeneratedAt:dd/MM/yyyy HH:mm}")
                    .FontSize(7).FontColor("#64748B");

                row.ConstantItem(120).AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(s => s.FontSize(7).FontColor("#64748B"));
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });
    }

    private void ComposeEntriesSection(IContainer container)
    {
        container.Table(table =>
        {
            DefineMovementColumns(table);

            table.Header(header =>
            {
                header.Cell().ColumnSpan(12)
                    .Background(SectionBarBg)
                    .Border(0.5f).BorderColor(SectionBarBg)
                    .PaddingVertical(4).PaddingHorizontal(6)
                    .Text("Compras").Bold().FontSize(8.5f).FontColor(Colors.White);

                string[] titles =
                [
                    "Fecha", "No Doc", "Tipo", "Producto", "Concepto", "Proveedor",
                    "Referencia", "UNIDADES", "COSTO", "Total", "Costo Mínimo", "Costo Máximo"
                ];

                for (var i = 0; i < titles.Length; i++)
                {
                    var isNumeric = i >= 7;
                    var cell = header.Cell()
                        .Background(HeaderBg)
                        .Border(0.5f).BorderColor(BorderHex)
                        .PaddingVertical(4).PaddingHorizontal(4);

                    if (isNumeric)
                        cell.AlignRight().Text(titles[i]).Bold().FontSize(7.5f);
                    else
                        cell.AlignLeft().Text(titles[i]).Bold().FontSize(7.5f);
                }
            });

            if (report.Entradas.Count == 0)
            {
                table.Cell().ColumnSpan(12)
                    .Border(0.5f).BorderColor(BorderHex)
                    .PaddingVertical(6).PaddingHorizontal(6)
                    .AlignCenter()
                    .Text("Sin movimientos de compras en el período seleccionado.")
                    .Italic().FontColor("#64748B");
            }
            else
            {
                for (var idx = 0; idx < report.Entradas.Count; idx++)
                {
                    var item = report.Entradas[idx];
                    var bg = idx % 2 == 1 ? AltRowBg : "#FFFFFF";

                    AddTextCell(table, item.Date.ToString("dd/MM/yyyy", InvariantFmt), bg);
                    AddTextCell(table, item.DocumentCode ?? "—", bg);
                    AddTextCell(table, "Compra", bg);
                    AddTextCell(table, item.StockCode ?? "—", bg);
                    AddTextCell(table, item.ProductName ?? "—", bg);
                    AddTextCell(table, item.Supplier ?? "—", bg);
                    AddTextCell(table, item.Reference ?? "—", bg);
                    AddNumberCell(table, FormatUnits(item.Units), bg);
                    AddNumberCell(table, FormatCurrency(item.Cost), bg);
                    AddNumberCell(table, FormatCurrency(item.Total), bg);
                    AddNumberCell(table, FormatCurrency(item.MinimumCost), bg);
                    AddNumberCell(table, FormatCurrency(item.MaximumCost), bg);
                }
            }

            // Subtotal row: TOTAL ENTRADA
            AddSubtotalCell(table, "", alignRight: false);
            AddSubtotalCell(table, "", alignRight: false);
            AddSubtotalCell(table, report.TotalEntrada.Count.ToString(InvariantFmt), alignRight: true);
            AddSubtotalCell(table, "", alignRight: false);
            AddSubtotalCell(table, "", alignRight: false);
            AddSubtotalCell(table, "", alignRight: false);
            AddSubtotalCell(table, "TOTAL ENTRADA", alignRight: false);
            AddSubtotalCell(table, FormatUnits(report.TotalEntrada.Units), alignRight: true);
            AddSubtotalCell(table, "", alignRight: true);
            AddSubtotalCell(table, FormatCurrency(report.TotalEntrada.Total), alignRight: true);
            AddSubtotalCell(table, "", alignRight: true);
            AddSubtotalCell(table, "", alignRight: true);
        });
    }

    private void ComposeExitsSection(IContainer container)
    {
        container.Table(table =>
        {
            DefineMovementColumns(table);

            table.Header(header =>
            {
                header.Cell().ColumnSpan(12)
                    .Background(SectionBarBg)
                    .Border(0.5f).BorderColor(SectionBarBg)
                    .PaddingVertical(4).PaddingHorizontal(6)
                    .Text("Salidas").Bold().FontSize(8.5f).FontColor(Colors.White);

                string[] titles =
                [
                    "Fecha", "No Doc", "Tipo", "Producto", "Concepto", "OS#",
                    "Referencia", "UNIDADES", "COSTO", "Total", "", "Costo Máximo"
                ];

                for (var i = 0; i < titles.Length; i++)
                {
                    var isNumeric = i >= 7;
                    var cell = header.Cell()
                        .Background(HeaderBg)
                        .Border(0.5f).BorderColor(BorderHex)
                        .PaddingVertical(4).PaddingHorizontal(4);

                    if (isNumeric)
                        cell.AlignRight().Text(titles[i]).Bold().FontSize(7.5f);
                    else
                        cell.AlignLeft().Text(titles[i]).Bold().FontSize(7.5f);
                }
            });

            if (report.Salidas.Count == 0)
            {
                table.Cell().ColumnSpan(12)
                    .Border(0.5f).BorderColor(BorderHex)
                    .PaddingVertical(6).PaddingHorizontal(6)
                    .AlignCenter()
                    .Text("Sin movimientos de salidas en el período seleccionado.")
                    .Italic().FontColor("#64748B");
            }
            else
            {
                for (var idx = 0; idx < report.Salidas.Count; idx++)
                {
                    var item = report.Salidas[idx];
                    var bg = idx % 2 == 1 ? AltRowBg : "#FFFFFF";

                    AddTextCell(table, item.Date.ToString("dd/MM/yyyy", InvariantFmt), bg);
                    AddTextCell(table, item.DocumentCode ?? "—", bg);
                    AddTextCell(table, "Salida", bg);
                    AddTextCell(table, item.StockCode ?? "—", bg);
                    AddTextCell(table, item.ProductName ?? "—", bg);
                    AddTextCell(table, item.ServiceOrderCode ?? "—", bg);
                    AddTextCell(table, item.Reference ?? "—", bg);
                    AddNumberCell(table, FormatUnits(item.Units), bg);
                    AddNumberCell(table, FormatCurrency(item.Cost), bg);
                    AddNumberCell(table, FormatCurrency(item.Total), bg);
                    AddTextCell(table, "", bg);
                    AddNumberCell(table, FormatCurrency(item.MaximumCost), bg);
                }
            }

            // Subtotal row: TOTAL SALIDA
            AddSubtotalCell(table, "", alignRight: false);
            AddSubtotalCell(table, "", alignRight: false);
            AddSubtotalCell(table, report.TotalSalida.Count.ToString(InvariantFmt), alignRight: true);
            AddSubtotalCell(table, "", alignRight: false);
            AddSubtotalCell(table, "", alignRight: false);
            AddSubtotalCell(table, "", alignRight: false);
            AddSubtotalCell(table, "TOTAL SALIDA", alignRight: false);
            AddSubtotalCell(table, FormatUnits(report.TotalSalida.Units), alignRight: true);
            AddSubtotalCell(table, "", alignRight: true);
            AddSubtotalCell(table, FormatCurrency(report.TotalSalida.Total), alignRight: true);
            AddSubtotalCell(table, "", alignRight: true);
            AddSubtotalCell(table, "", alignRight: true);
        });
    }

    private void ComposeProductMovementSummary(IContainer container)
    {
        container.ShowEntire().Table(table =>
        {
            DefineMovementColumns(table);

            table.Cell().ColumnSpan(7)
                .Background(SectionBarBg)
                .Border(0.5f).BorderColor(SectionBarBg)
                .PaddingVertical(5).PaddingHorizontal(6)
                .Text("Movimiento del producto").Bold().FontSize(8.5f).FontColor(Colors.White);

            table.Cell()
                .Background(SectionBarBg)
                .Border(0.5f).BorderColor(SectionBarBg)
                .PaddingVertical(5).PaddingHorizontal(4)
                .AlignRight()
                .Text(FormatUnits(report.MovimientoProducto.Units)).Bold().FontSize(8f).FontColor(Colors.White);

            table.Cell()
                .Background(SectionBarBg)
                .Border(0.5f).BorderColor(SectionBarBg)
                .PaddingVertical(5).PaddingHorizontal(4)
                .Text("");

            table.Cell()
                .Background(SectionBarBg)
                .Border(0.5f).BorderColor(SectionBarBg)
                .PaddingVertical(5).PaddingHorizontal(4)
                .AlignRight()
                .Text(FormatCurrency(report.MovimientoProducto.Total)).Bold().FontSize(8f).FontColor(Colors.White);

            table.Cell().ColumnSpan(2)
                .Background(SectionBarBg)
                .Border(0.5f).BorderColor(SectionBarBg)
                .PaddingVertical(5).PaddingHorizontal(4)
                .Text("");
        });
    }

    private static void DefineMovementColumns(TableDescriptor table)
    {
        table.ColumnsDefinition(cols =>
        {
            cols.RelativeColumn(0.95f); // Fecha
            cols.RelativeColumn(0.95f); // No Doc
            cols.RelativeColumn(0.65f); // Tipo
            cols.RelativeColumn(1.10f); // Producto (SKU)
            cols.RelativeColumn(1.55f); // Concepto
            cols.RelativeColumn(1.25f); // Proveedor / OS#
            cols.RelativeColumn(1.05f); // Referencia
            cols.RelativeColumn(0.75f); // UNIDADES
            cols.RelativeColumn(0.85f); // COSTO
            cols.RelativeColumn(1.00f); // Total
            cols.RelativeColumn(0.90f); // Costo Mínimo
            cols.RelativeColumn(0.90f); // Costo Máximo
        });
    }

    private static void AddTextCell(TableDescriptor table, string value, string bg)
    {
        table.Cell()
            .Background(bg)
            .Border(0.5f).BorderColor(BorderHex)
            .PaddingVertical(3.5f).PaddingHorizontal(4)
            .AlignLeft().AlignMiddle()
            .Text(value);
    }

    private static void AddNumberCell(TableDescriptor table, string value, string bg)
    {
        table.Cell()
            .Background(bg)
            .Border(0.5f).BorderColor(BorderHex)
            .PaddingVertical(3.5f).PaddingHorizontal(4)
            .AlignRight().AlignMiddle()
            .Text(value);
    }

    private static void AddSubtotalCell(TableDescriptor table, string value, bool alignRight)
    {
        var cell = table.Cell()
            .Background(SubtotalBg)
            .Border(0.5f).BorderColor(BorderHex)
            .PaddingVertical(4).PaddingHorizontal(4)
            .AlignMiddle();

        if (alignRight)
            cell.AlignRight().Text(value).Bold().FontSize(7.5f);
        else
            cell.AlignLeft().Text(value).Bold().FontSize(7.5f);
    }

    private static string FormatCurrency(decimal value) => value.ToString("#,##0.00", InvariantFmt);

    private static string FormatUnits(decimal value) =>
        value == Math.Truncate(value)
            ? value.ToString("#,##0", InvariantFmt)
            : value.ToString("#,##0.##", InvariantFmt);
}
