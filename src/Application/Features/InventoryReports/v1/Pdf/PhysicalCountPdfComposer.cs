using System.Globalization;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Pdf
{
    public static class PhysicalCountPdfComposer
    {
        private sealed record GroupInfo(string CountedBy, string CountedAt, int RowCount);

        private const int MaxGroupRows = 5;

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
        private static readonly Color AlternateRowColor = Color.FromHex("#F9FAFB");
        private static readonly Color FooterColor = Color.FromHex("#9CA3AF");
        private static readonly Color White = Color.FromHex("#FFFFFF");

        static PhysicalCountPdfComposer()
        {
            QuestPDF.Settings.UseSystemFonts = true;
            QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
        }

        public static Action<IDocumentContainer> Compose(
            PhysicalCountLinesPageDto page,
            string generatedBy,
            DateTime generatedAt,
            Image? logoImage)
        {
            var totalItems = page.Data.Count;
            var completedItems = page.Data.Count(line =>
                !line.Difference.HasValue || line.Difference.Value == 0);
            var itemsWithDifferences = page.Data.Count(line =>
                line.Difference.HasValue && line.Difference.Value != 0);

            var groups = BuildGroups(page.Data);

            return container =>
            {
                container.Page(descriptor =>
                {
                    descriptor.Size(PageSizes.A4.Portrait());
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
                                    .Text("REPORTE DE LEVANTAMIENTO FÍSICO")
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
                                text.Span($"   Items totales: {totalItems}").FontColor(SubtitleColor);
                                text.Span("   |   ").FontColor(BorderColor);
                                text.Span($"Items completos: {completedItems}").FontColor(SubtitleColor);
                                text.Span("   |   ").FontColor(BorderColor);
                                text.Span($"Items con diferencias: {itemsWithDifferences}").FontColor(SubtitleColor);
                            });

                        if (page.Data.Count == 0)
                        {
                            column.Item()
                                .Text("No hay datos para este levantamiento.")
                                .FontColor(SubtitleColor);
                        }
                        else
                        {
                            column.Item()
                            .Border(1)
                            .BorderColor(BorderColor)
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(86);   // Código
                                    columns.RelativeColumn(16);   // Producto
                                    columns.RelativeColumn(8);    // Unidad
                                    columns.RelativeColumn(9);    // Stock sistema
                                    columns.RelativeColumn(9);    // Stock físico
                                    columns.RelativeColumn(11);   // Diferencia
                                    columns.RelativeColumn(20);   // Observaciones
                                    columns.RelativeColumn(13);   // Contado por
                                    columns.RelativeColumn(11);   // Hora conteo
                                });

                                table.Header(AddHeaderRow);

                                for (var index = 0; index < page.Data.Count;)
                                {
                                    if (groups.TryGetValue(index, out var group))
                                    {
                                        for (var offset = 0; offset < group.RowCount; offset++)
                                        {
                                            AddRegularCells(table, DataCellStyle, page.Data[index + offset]);

                                            if (offset == 0)
                                                AddMergedCells(table, MergedCellStyle, group);
                                        }

                                        index += group.RowCount;
                                    }
                                    else
                                    {
                                        AddRegularCells(table, DataCellStyle, page.Data[index]);
                                        AddEmptyTrailingCells(table, DataCellStyle);
                                        index++;
                                    }
                                }
                            });
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

        private static Dictionary<int, GroupInfo> BuildGroups(IReadOnlyList<PhysicalCountLineDto> lines)
        {
            var result = new Dictionary<int, GroupInfo>();

            for (var start = 0; start < lines.Count;)
            {
                var key = BuildGroupKey(lines[start]);
                var runEnd = start + 1;

                while (runEnd < lines.Count && BuildGroupKey(lines[runEnd]) == key)
                    runEnd++;

                for (var blockStart = start; blockStart < runEnd; blockStart += MaxGroupRows)
                {
                    var blockEnd = Math.Min(blockStart + MaxGroupRows, runEnd);
                    var countedByName = lines[blockStart].CountedByName;
                    var countedBy = string.IsNullOrWhiteSpace(countedByName)
                        ? "—"
                        : countedByName.Trim();

                    var countedAtValue = lines[blockStart].CountedAt;
                    var countedAt = countedAtValue.HasValue
                        ? FormatCountedDateTime(countedAtValue.Value)
                        : "—";

                    result[blockStart] = new GroupInfo(
                        countedBy,
                        countedAt,
                        blockEnd - blockStart);
                }

                start = runEnd;
            }

            return result;
        }

        private static string BuildGroupKey(PhysicalCountLineDto line)
        {
            var countedByName = line.CountedByName;
            var countedBy = string.IsNullOrWhiteSpace(countedByName)
                ? "—"
                : countedByName.Trim();

            var countedAt = line.CountedAt.HasValue
                ? line.CountedAt.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                : "—";

            return $"{countedBy}\u001F{countedAt}";
        }

        private static void AddHeaderRow(TableCellDescriptor header)
        {
            AddHeaderCell(header, "Código");
            AddHeaderCell(header, "Producto");
            AddHeaderCell(header, "Unidad");
            AddHeaderCell(header, "Stock sistema", alignRight: true);
            AddHeaderCell(header, "Stock físico", alignRight: true);
            AddHeaderCell(header, "Diferencia", alignRight: true);
            AddHeaderCell(header, "Observaciones");
            AddHeaderCell(header, "Contado por", alignCenter: true);
            AddHeaderCell(header, "Hora conteo", alignCenter: true);
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
        private static void AddRegularCells(
            TableDescriptor table,
            Func<IContainer, IContainer> cellStyle,
            PhysicalCountLineDto line)
        {
            table.Cell().Element(cellStyle).Text(line.ProductCode ?? "—");
            table.Cell().Element(cellStyle).Text(line.ProductName ?? "—");
            table.Cell().Element(cellStyle).Text(line.UnitMeasure ?? "—");
            table.Cell().Element(cellStyle).Text(FormatNumber(line.SystemStock)).AlignRight();
            table.Cell().Element(cellStyle).Text(FormatNumber(line.PhysicalStock)).AlignRight();
            table.Cell().Element(cellStyle).Text(FormatNumber(line.Difference)).AlignRight();
            table.Cell().Element(cellStyle).Text(line.Observations ?? "—");
        }

        private static void AddMergedCells(
            TableDescriptor table,
            Func<IContainer, IContainer> cellStyle,
            GroupInfo group)
        {
            var rowSpan = (uint)group.RowCount;

            table.Cell()
                .RowSpan(rowSpan)
                .Element(cellStyle)
                .AlignCenter()
                .Text(group.CountedBy)
                .Bold();

            table.Cell()
                .RowSpan(rowSpan)
                .Element(cellStyle)
                .AlignCenter()
                .Text(group.CountedAt);
        }

        private static void AddEmptyTrailingCells(
            TableDescriptor table,
            Func<IContainer, IContainer> cellStyle)
        {
            table.Cell().Element(cellStyle).Text(string.Empty);
            table.Cell().Element(cellStyle).Text(string.Empty);
        }

        private static string FormatNumber(decimal? value) =>
            value?.ToString("N", NumberFormat) ?? "—";

        private static string FormatCountedDateTime(DateTime value) =>
            value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

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

        private static IContainer HeaderRowStyle(IContainer container) =>
            container
                .Background(HeaderBackground)
                .BorderBottom(1)
                .BorderColor(HeaderBorderColor);

        private static IContainer HeaderCellStyle(IContainer container) =>
        container
            .Background(HeaderBackground)
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

        private static IContainer AlternateDataCellStyle(IContainer container) =>
            container
                .Background(AlternateRowColor)
                .MinHeight(16)
                .AlignMiddle()
                .BorderBottom(1)
                .BorderColor(RowBorderColor)
                .DefaultTextStyle(style => style.FontSize(8));

        private static IContainer MergedCellStyle(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderLeft(1)
            .BorderColor(RowBorderColor)
            .PaddingVertical(6)
            .PaddingHorizontal(5)
            .AlignMiddle()
            .DefaultTextStyle(style => style.FontSize(8));

        private static IContainer AlternateMergedCellStyle(IContainer container) =>
            container
                .Background(AlternateRowColor)
                .Border(1, RowBorderColor)
                .AlignMiddle()
                .DefaultTextStyle(style => style.FontSize(8));
    }
}
