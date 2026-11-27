using System.Globalization;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Pdf;

public static class KardexPdfComposer
{
    private static readonly CultureInfo SpanishNicaragua = CultureInfo.GetCultureInfo("es-NI");

    private static readonly NumberFormatInfo NumberFormat = new()
    {
        NumberDecimalSeparator = ".",
        NumberGroupSeparator = ",",
        NumberDecimalDigits = 2
    };

    private static readonly string[] ReportFontFamilies = ["Helvetica", "Arial"];

    private static readonly Color TitleColor = Color.FromHex("#111827");
    private static readonly Color SubtitleColor = Color.FromHex("#6B7280");
    private static readonly Color BorderColor = Color.FromHex("#D1D5DB");
    private static readonly Color HeaderBackground = Color.FromHex("#1D4ED8");
    private static readonly Color HeaderBorderColor = Color.FromHex("#1E40AF");
    private static readonly Color RowBorderColor = Color.FromHex("#E5E7EB");
    private static readonly Color SummaryBackground = Color.FromHex("#EFF6FF");
    private static readonly Color FooterColor = Color.FromHex("#9CA3AF");
    private static readonly Color White = Color.FromHex("#FFFFFF");

    static KardexPdfComposer()
    {
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    public static Action<IDocumentContainer> Compose(
        KardexReportDto report,
        string generatedBy,
        DateTime generatedAt,
        Image? logoImage)
    {
        var totalRows = report.Products.Sum(product => product.Rows.Count);

        return container =>
        {
            container.Page(descriptor =>
            {
                descriptor.Size(PageSizes.A4.Landscape());
                descriptor.Margin(24);
                descriptor.DefaultTextStyle(style => style
                    .FontFamily(ReportFontFamilies)
                    .FontSize(9));

                descriptor.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        if (logoImage is not null)
                        {
                            row.ConstantItem(70).Height(40).Image(logoImage).FitArea();
                        }
                        else
                        {
                            row.ConstantItem(70).Height(40);
                        }

                        row.RelativeItem().Column(column =>
                        {
                            column.Item()
                                .AlignCenter()
                                .Text("REPORTE DE KARDEX")
                                .FontSize(14)
                                .Bold()
                                .FontColor(TitleColor);

                            column.Item()
                                .PaddingTop(2)
                                .AlignCenter()
                                .Text($"Generado: {FormatSpanishDateTime(generatedAt)}")
                                .FontColor(SubtitleColor);

                            column.Item()
                                .PaddingTop(2)
                                .AlignCenter()
                                .Text($"Generado por: {generatedBy}")
                                .FontColor(SubtitleColor);
                        });

                        row.ConstantItem(70).Height(40);
                    });

                    header.Item()
                        .PaddingTop(12)
                        .BorderBottom(1)
                        .BorderColor(BorderColor);
                });

                descriptor.Content().Column(column =>
                {
                    column.Item()
                        .PaddingTop(10)
                        .PaddingBottom(10)
                        .Text(text =>
                        {
                            text.Span("Resumen").Bold().FontColor(TitleColor);
                            text.Span($"   Productos: {report.Products.Count}").FontColor(SubtitleColor);
                            text.Span("   |   ").FontColor(BorderColor);
                            text.Span($"Movimientos: {totalRows}").FontColor(SubtitleColor);
                            text.Span("   |   ").FontColor(BorderColor);
                            text.Span($"Del {FormatDate(report.From)} Al {FormatDate(report.To)}")
                                .FontColor(SubtitleColor);
                        });

                    if (report.Products.Count == 0)
                    {
                        column.Item()
                            .Text("No hay datos para el periodo seleccionado.")
                            .FontColor(SubtitleColor);
                    }
                    else
                    {
                        foreach (var product in report.Products)
                        {
                            column.Item().PaddingTop(8).Element(containerItem =>
                                ComposeProductHeader(containerItem, product));

                            column.Item().PaddingTop(4).Element(containerItem =>
                                ComposeMovementsTable(containerItem, product));
                        }
                    }

                    column.Item()
                        .PaddingTop(12)
                        .AlignCenter()
                        .Text("Documento generado para impresión")
                        .FontSize(8)
                        .FontColor(FooterColor);
                });
            });
        };
    }

    private static void ComposeProductHeader(IContainer container, KardexProductReportDto product)
    {
        container
            .Border(1)
            .BorderColor(BorderColor)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(20); // Producto
                    columns.RelativeColumn(13); // Categoría
                    columns.RelativeColumn(7);  // Unidad
                    columns.RelativeColumn(10); // Saldo inicial kardex
                    columns.RelativeColumn(10); // Inventario inicial
                    columns.RelativeColumn(10); // Costo promedio inicial
                });

                table.Header(header =>
                {
                    AddHeaderCell(header, "Producto (código, insumo)");
                    AddHeaderCell(header, "Categoría");
                    AddHeaderCell(header, "Unidad");
                    AddHeaderCell(header, "Saldo inicial kardex", alignRight: true);
                    AddHeaderCell(header, "Inventario inicial", alignRight: true);
                    AddHeaderCell(header, "Costo promedio inicial", alignRight: true);
                });

                var productLabel = string.IsNullOrWhiteSpace(product.ProductCode)
                    ? FormatText(product.ProductName)
                    : $"{product.ProductCode} - {FormatText(product.ProductName)}";

                table.Cell().Element(DataCellStyle).Text(productLabel);
                table.Cell().Element(DataCellStyle).Text(FormatText(product.CategoryName));
                table.Cell().Element(DataCellStyle).Text(FormatText(product.UnitMeasureSymbol));
                table.Cell().Element(DataCellStyle)
                    .Text(FormatNumber(product.InitialKardexBalance)).AlignRight();
                table.Cell().Element(DataCellStyle)
                    .Text(FormatNumber(product.InitialInventory)).AlignRight();
                table.Cell().Element(DataCellStyle)
                    .Text(FormatNumber(product.InitialAverageCost)).AlignRight();
            });
    }

    private static void ComposeMovementsTable(IContainer container, KardexProductReportDto product)
    {
        container
            .Border(1)
            .BorderColor(BorderColor)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(7);  // Tipo
                    columns.RelativeColumn(10); // Fecha
                    columns.RelativeColumn(12); // N° registro
                    columns.RelativeColumn(6);  // Documento
                    columns.RelativeColumn(7);  // Cantidad entrada
                    columns.RelativeColumn(7);  // Cantidad salida
                    columns.RelativeColumn(8);  // Costo unitario
                    columns.RelativeColumn(9);  // Total entrada
                    columns.RelativeColumn(9);  // Total salida
                    columns.RelativeColumn(8);  // Saldo unidades
                    columns.RelativeColumn(8);  // Costo promedio
                    columns.RelativeColumn(10); // Saldo kardex
                });

                table.Header(AddHeaderRow);

                foreach (var row in product.Rows)
                {
                    table.Cell().Element(DataCellStyle).Text(row.MovementType);
                    table.Cell().Element(DataCellStyle).Text(FormatDate(row.Date));
                    table.Cell().Element(DataCellStyle).Text(FormatText(row.RecordNumber));
                    table.Cell().Element(DataCellStyle).AlignCenter().Text(FormatReference(row.Document));
                    table.Cell().Element(DataCellStyle)
                        .Text(FormatNumber(row.EntryUnits)).AlignRight();
                    table.Cell().Element(DataCellStyle)
                        .Text(FormatNumber(row.ExitUnits)).AlignRight();
                    table.Cell().Element(DataCellStyle)
                        .Text(FormatNumber(row.UnitCost)).AlignRight();
                    table.Cell().Element(DataCellStyle)
                        .Text(FormatNumber(row.TotalEntryCost)).AlignRight();
                    table.Cell().Element(DataCellStyle)
                        .Text(FormatNumber(row.TotalExitCost)).AlignRight();
                    table.Cell().Element(DataCellStyle)
                        .Text(FormatNumber(row.BalanceUnits)).AlignRight();
                    table.Cell().Element(DataCellStyle)
                        .Text(FormatNumber(row.AverageCost)).AlignRight();
                    table.Cell().Element(DataCellStyle)
                        .Text(FormatNumber(row.KardexBalance)).AlignRight();
                }

                var summary = product.Summary;

                table.Cell()
                    .ColumnSpan(4)
                    .Element(SummaryCellStyle)
                    .AlignRight()
                    .Text($"Cantidad de filas: {summary.RowCount}");
                table.Cell().Element(SummaryCellStyle).Text(FormatNumber(summary.TotalEntries)).AlignRight();
                table.Cell().Element(SummaryCellStyle).Text(FormatNumber(summary.TotalExits)).AlignRight();
                table.Cell().Element(SummaryCellStyle).Text(string.Empty);
                table.Cell().Element(SummaryCellStyle).Text(FormatNumber(summary.TotalEntryCost)).AlignRight();
                table.Cell().Element(SummaryCellStyle).Text(FormatNumber(summary.TotalExitCost)).AlignRight();
                table.Cell().Element(SummaryCellStyle).Text(FormatNumber(summary.FinalUnits)).AlignRight();
                table.Cell().Element(SummaryCellStyle).Text(FormatNumber(summary.FinalAverageCost)).AlignRight();
                table.Cell().Element(SummaryCellStyle).Text(FormatNumber(summary.FinalKardexBalance)).AlignRight();
            });
    }

    private static void AddHeaderRow(TableCellDescriptor header)
    {
        AddHeaderCell(header, "Tipo");
        AddHeaderCell(header, "Fecha");
        AddHeaderCell(header, "N° registro");
        AddHeaderCell(header, "Documento", alignCenter: true);
        AddHeaderCell(header, "Cant. entrada", alignRight: true);
        AddHeaderCell(header, "Cant. salida", alignRight: true);
        AddHeaderCell(header, "Costo unitario", alignRight: true);
        AddHeaderCell(header, "Total entrada", alignRight: true);
        AddHeaderCell(header, "Total salida", alignRight: true);
        AddHeaderCell(header, "Saldo und.", alignRight: true);
        AddHeaderCell(header, "Costo prom.", alignRight: true);
        AddHeaderCell(header, "Saldo kardex", alignRight: true);
    }

    private static void AddHeaderCell(
        TableCellDescriptor header,
        string text,
        bool alignRight = false,
        bool alignCenter = false)
    {
        var cell = header.Cell()
            .Element(HeaderCellStyle)
            .Text(text);

        if (alignRight) cell.AlignRight();
        else if (alignCenter) cell.AlignCenter();
    }

    private static string FormatNumber(decimal value) => value.ToString("N", NumberFormat);

    private static string FormatText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    private static string FormatReference(ReferenceType reference) =>
        reference == ReferenceType.None ? "—" : reference.ToString();

    private static string FormatDate(DateTime value) =>
        value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string FormatSpanishDateTime(DateTime value)
    {
        var weekday = SpanishNicaragua.DateTimeFormat.GetDayName(value.DayOfWeek);
        if (string.IsNullOrEmpty(weekday))
            return value.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

        weekday = char.ToUpper(weekday[0], SpanishNicaragua) + weekday[1..];
        var hour = value.Hour % 12 == 0 ? 12 : value.Hour % 12;
        var period = value.Hour >= 12 ? "pm" : "am";

        return $"{weekday}, {FormatDate(value)} {hour:00}:{value.Minute:00}:{value.Second:00} {period}";
    }

    private static IContainer HeaderCellStyle(IContainer container) =>
        container
            .Background(HeaderBackground)
            .BorderRight(1)
            .BorderColor(HeaderBorderColor)
            .PaddingVertical(7)
            .PaddingHorizontal(5)
            .AlignMiddle()
            .DefaultTextStyle(style => style.FontSize(8).FontColor(White).Bold());

    private static IContainer DataCellStyle(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(RowBorderColor)
            .PaddingVertical(6)
            .PaddingHorizontal(5)
            .AlignMiddle()
            .DefaultTextStyle(style => style.FontSize(8));

    private static IContainer SummaryCellStyle(IContainer container) =>
        container
            .Background(SummaryBackground)
            .BorderTop(1)
            .BorderColor(HeaderBorderColor)
            .PaddingVertical(6)
            .PaddingHorizontal(5)
            .AlignMiddle()
            .DefaultTextStyle(style => style.FontSize(8).Bold().FontColor(TitleColor));
}
