using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Excel;

public static class KardexExcelComposer
{
    public const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly XNamespace MainNs =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelNs =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PkgRelNs =
        "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypesNs =
        "http://schemas.openxmlformats.org/package/2006/content-types";

    // Fixed style indices matching Kardex (1).xlsx styles.xml
    private const int StyleDefault = 0;
    private const int StyleColDefaultMiddle = 1;
    private const int StyleColCenterMiddle = 2;
    private const int StyleNumRight2 = 4;
    private const int StyleTextLeft = 5;
    private const int StyleTextCenter = 6;
    private const int StyleTotalNumRight = 7;
    private const int StyleTotalIntRight = 8;
    private const int StyleTotalNumCenter = 9;
    private const int StyleTotalIntCenter = 10;
    private const int StyleAccounting2 = 11;
    private const int StyleUnits3 = 12;
    private const int StyleUnitCost2 = 13;
    private const int StyleMovementQtyCenter = 14;
    private const int StyleRecordDocCenter = 15;
    private const int StyleDateLeft = 16;
    private const int StyleRowHeaderBase = 17;
    private const int StyleTableHeader = 23;
    private const int StyleLineaBanner = 24;
    private const int StyleTotalLabel = 25;
    private const int StyleBalanceLabelRight = 26;
    private const int StyleAvgCostLabelCenter = 27;
    private const int StyleUmBox = 28;
    private const int StyleKardexLabelCenter = 29;
    private const int StyleKardexCurrencyCenter = 30;
    private const int StyleHeaderSubtitle = 31;
    private const int StyleHeaderKardexTitle = 32;
    private const int StyleHeaderCompanyTitle = 33;
    private const int StyleProductBanner = 34;
    private const int StyleInitialCostCurrency = 21;
    private const int StyleFinalCostCurrency = 22;

    private const int BaseDynamicUnitStyleIndex = 35;

    private sealed record SheetSpec(
        string SheetName,
        KardexProductReportDto? Product,
        int UnitStyleIndex);

    public static byte[] Compose(KardexReportDto report, string defaultWarehouseName = "Bodega de suministro")
    {
        var products = report.Products;
        var unitSymbols = new List<string>();
        var unitStyleBySymbol = new Dictionary<string, int>(StringComparer.Ordinal);

        int ResolveUnitStyleIndex(string? rawSymbol)
        {
            var symbol = SanitizeFormatLiteral(rawSymbol);
            if (unitStyleBySymbol.TryGetValue(symbol, out var existing))
                return existing;

            var idx = BaseDynamicUnitStyleIndex + unitSymbols.Count;
            unitSymbols.Add(symbol);
            unitStyleBySymbol[symbol] = idx;
            return idx;
        }

        var sheets = new List<SheetSpec>();
        if (products.Count == 0)
        {
            sheets.Add(new SheetSpec("KARDEX", null, ResolveUnitStyleIndex(null)));
        }
        else if (products.Count == 1)
        {
            var only = products[0];
            sheets.Add(new SheetSpec("KARDEX", only, ResolveUnitStyleIndex(only.UnitMeasureSymbol)));
        }
        else
        {
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < products.Count; i++)
            {
                var p = products[i];
                var sheetName = BuildUniqueSheetName(p.ProductCode, i + 1, usedNames);
                sheets.Add(new SheetSpec(sheetName, p, ResolveUnitStyleIndex(p.UnitMeasureSymbol)));
            }
        }

        var companyName = string.IsNullOrWhiteSpace(report.CompanyName)
            ? "ALPAC, S.A."
            : report.CompanyName.Trim();

        var periodText = FormatPeriodRange(report.From, report.To);

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteXmlEntry(archive, "[Content_Types].xml", BuildContentTypesXml(sheets.Count));
            WriteXmlEntry(archive, "_rels/.rels", BuildRootRelsXml());
            WriteXmlEntry(archive, "xl/workbook.xml", BuildWorkbookXml(sheets));
            WriteXmlEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelsXml(sheets.Count));
            WriteXmlEntry(archive, "xl/theme/theme1.xml", XDocument.Parse(OfficeThemeXml));
            WriteXmlEntry(archive, "xl/styles.xml", BuildStylesXml(unitSymbols));

            for (var i = 0; i < sheets.Count; i++)
            {
                var spec = sheets[i];
                var sheetXml = BuildWorksheetXml(
                    companyName,
                    periodText,
                    defaultWarehouseName,
                    spec.Product,
                    spec.UnitStyleIndex);
                WriteXmlEntry(archive, $"xl/worksheets/sheet{i + 1}.xml", sheetXml);
            }
        }

        return ms.ToArray();
    }

    private static XDocument BuildContentTypesXml(int sheetCount)
    {
        var overrides = new List<XElement>
        {
            new(ContentTypesNs + "Override",
                new XAttribute("PartName", "/xl/workbook.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
            new(ContentTypesNs + "Override",
                new XAttribute("PartName", "/xl/theme/theme1.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.theme+xml")),
            new(ContentTypesNs + "Override",
                new XAttribute("PartName", "/xl/styles.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"))
        };

        for (var i = 1; i <= sheetCount; i++)
        {
            overrides.Add(new XElement(ContentTypesNs + "Override",
                new XAttribute("PartName", $"/xl/worksheets/sheet{i}.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")));
        }

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(ContentTypesNs + "Types",
                new XElement(ContentTypesNs + "Default",
                    new XAttribute("Extension", "rels"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(ContentTypesNs + "Default",
                    new XAttribute("Extension", "xml"),
                    new XAttribute("ContentType", "application/xml")),
                overrides));
    }

    private static XDocument BuildRootRelsXml() =>
        new(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(PkgRelNs + "Relationships",
                new XElement(PkgRelNs + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                    new XAttribute("Target", "xl/workbook.xml"))));

    private static XDocument BuildWorkbookXml(List<SheetSpec> sheets)
    {
        var sheetsElement = new XElement(MainNs + "sheets");
        for (var i = 0; i < sheets.Count; i++)
        {
            sheetsElement.Add(new XElement(MainNs + "sheet",
                new XAttribute("name", sheets[i].SheetName),
                new XAttribute("sheetId", i + 1),
                new XAttribute(RelNs + "id", $"rId{i + 1}")));
        }

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(MainNs + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", RelNs),
                new XElement(MainNs + "bookViews",
                    new XElement(MainNs + "workbookView",
                        new XAttribute("activeTab", 0))),
                sheetsElement,
                new XElement(MainNs + "calcPr",
                    new XAttribute("calcId", "191028"),
                    new XAttribute("fullCalcOnLoad", "1"))));
    }

    private static XDocument BuildWorkbookRelsXml(int sheetCount)
    {
        var rels = new List<XElement>();
        for (var i = 1; i <= sheetCount; i++)
        {
            rels.Add(new XElement(PkgRelNs + "Relationship",
                new XAttribute("Id", $"rId{i}"),
                new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                new XAttribute("Target", $"worksheets/sheet{i}.xml")));
        }

        rels.Add(new XElement(PkgRelNs + "Relationship",
            new XAttribute("Id", $"rId{sheetCount + 1}"),
            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme"),
            new XAttribute("Target", "theme/theme1.xml")));

        rels.Add(new XElement(PkgRelNs + "Relationship",
            new XAttribute("Id", $"rId{sheetCount + 2}"),
            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),
            new XAttribute("Target", "styles.xml")));

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(PkgRelNs + "Relationships", rels));
    }

    private static XDocument BuildStylesXml(List<string> unitSymbols)
    {
        var doc = XDocument.Parse(BaseStylesXml);
        var root = doc.Root!;

        var numFmts = root.Element(MainNs + "numFmts")!;
        var cellXfs = root.Element(MainNs + "cellXfs")!;

        var nextNumFmtId = 171;
        foreach (var symbol in unitSymbols)
        {
            var fmtCode = string.IsNullOrEmpty(symbol)
                ? "0.000"
                : $"0.000\"{symbol}\"";

            var fmtId = nextNumFmtId++;
            numFmts.Add(new XElement(MainNs + "numFmt",
                new XAttribute("numFmtId", fmtId),
                new XAttribute("formatCode", fmtCode)));

            cellXfs.Add(new XElement(MainNs + "xf",
                new XAttribute("numFmtId", fmtId),
                new XAttribute("fontId", 2),
                new XAttribute("fillId", 2),
                new XAttribute("borderId", 3),
                new XAttribute("xfId", 0),
                new XAttribute("applyNumberFormat", 1),
                new XAttribute("applyFont", 1),
                new XAttribute("applyFill", 1),
                new XAttribute("applyBorder", 1),
                new XAttribute("applyAlignment", 1),
                new XElement(MainNs + "alignment",
                    new XAttribute("horizontal", "center"),
                    new XAttribute("vertical", "center"))));
        }

        numFmts.SetAttributeValue("count", numFmts.Elements().Count());
        cellXfs.SetAttributeValue("count", cellXfs.Elements().Count());

        return doc;
    }

    private static XDocument BuildWorksheetXml(
        string companyName,
        string periodText,
        string defaultWarehouseName,
        KardexProductReportDto? product,
        int unitStyleIndex)
    {
        var warehouseName = !string.IsNullOrWhiteSpace(product?.WarehouseName)
            ? product!.WarehouseName!.Trim()
            : defaultWarehouseName;
        var categoryName = !string.IsNullOrWhiteSpace(product?.CategoryName)
            ? product!.CategoryName!.Trim()
            : "Sin línea";
        var productCode = !string.IsNullOrWhiteSpace(product?.ProductCode)
            ? product!.ProductCode!.Trim()
            : "—";
        var productName = !string.IsNullOrWhiteSpace(product?.ProductName)
            ? product!.ProductName!.Trim()
            : "—";
        var unitLabel = !string.IsNullOrWhiteSpace(product?.UnitMeasureName)
            ? $"U/M: {product!.UnitMeasureName!.Trim()}"
            : !string.IsNullOrWhiteSpace(product?.UnitMeasureSymbol)
                ? $"U/M: {product!.UnitMeasureSymbol!.Trim()}"
                : "U/M: —";

        var initialUnits = product?.InitialInventory ?? 0m;
        var initialKardex = product?.InitialKardexBalance ?? 0m;
        var initialAvgCost = product?.InitialAverageCost ?? 0m;
        var movementRows = product?.Rows ?? [];

        const int firstDataRow = 11;
        var dataRowCount = movementRows.Count;
        var lastDataRow = dataRowCount > 0 ? firstDataRow + dataRowCount - 1 : firstDataRow;
        var totalRow = firstDataRow + dataRowCount;
        var blankBeforeFinalRow = totalRow + 1;
        var finalSummaryRow = totalRow + 2;

        var sheetData = new XElement(MainNs + "sheetData");

        // Row 1: Company Name (A1:L1)
        sheetData.Add(BuildMergedBannerRow(1, 17.45, StyleHeaderCompanyTitle, (1, companyName)));

        // Row 2: "Kardex" (A2:L2)
        sheetData.Add(BuildMergedBannerRow(2, 15.6, StyleHeaderKardexTitle, (1, "Kardex")));

        // Row 3: Warehouse / Area Name (A3:L3)
        sheetData.Add(BuildMergedBannerRow(3, null, StyleHeaderSubtitle, (1, warehouseName)));

        // Row 4: Period "Del d/M/yyyy Al d/M/yyyy" (A4:L4)
        sheetData.Add(BuildMergedBannerRow(4, null, StyleHeaderSubtitle, (1, periodText)));

        // Row 5: Blank spacer
        sheetData.Add(new XElement(MainNs + "row",
            new XAttribute("r", 5),
            new XAttribute("spans", "1:12")));

        // Row 6: Línea (A6:D6) | Category (E6:L6)
        sheetData.Add(BuildMergedBannerRow(6, null, StyleLineaBanner, (1, "Línea"), (5, categoryName)));

        // Row 7: Producto: (A7:C7) | Code (D7:G7) | Product Name (H7:L7)
        sheetData.Add(BuildMergedBannerRow(
            7,
            null,
            StyleProductBanner,
            (1, "Producto:"),
            (4, productCode),
            (8, productName)));

        // Row 8: Initial Balances Summary Bar
        var initialKardexFormula = initialUnits > 0m ? "+I8*L8" : null;
        sheetData.Add(new XElement(MainNs + "row",
            new XAttribute("r", 8),
            new XAttribute("spans", "1:12"),
            new XAttribute("ht", "13.15"),
            StringCell("A8", StyleUmBox, unitLabel),
            EmptyCell("B8", StyleUmBox),
            StringCell("C8", StyleKardexLabelCenter, "Saldo Kardex Inicial:"),
            EmptyCell("D8", StyleKardexLabelCenter),
            NumberCell("E8", StyleKardexCurrencyCenter, initialKardex, initialKardexFormula),
            EmptyCell("F8", StyleKardexCurrencyCenter),
            StringCell("G8", StyleBalanceLabelRight, "Inventario Inicial:"),
            EmptyCell("H8", StyleBalanceLabelRight),
            NumberCell("I8", unitStyleIndex, initialUnits),
            StringCell("J8", StyleAvgCostLabelCenter, "Costo Promedio:"),
            EmptyCell("K8", StyleAvgCostLabelCenter),
            NumberCell("L8", StyleInitialCostCurrency, initialAvgCost)));

        // Row 9: Blank spacer
        sheetData.Add(new XElement(MainNs + "row",
            new XAttribute("r", 9),
            new XAttribute("spans", "1:12")));

        // Row 10: Table Headers
        var headers = new[]
        {
            "Concepto",
            "Fecha",
            "No. Registro",
            "Documento",
            "Entrada",
            "Salida",
            "Costo Unitario C$",
            "Total  Entrada C$",
            "Total Salida C$",
            "Saldo de Unidades",
            "Costo Promedio C$",
            "Saldo Kardex C$"
        };
        var row10 = new XElement(MainNs + "row",
            new XAttribute("r", 10),
            new XAttribute("spans", "1:12"),
            new XAttribute("s", StyleRowHeaderBase),
            new XAttribute("customFormat", 1),
            new XAttribute("ht", "22.5"),
            new XAttribute("customHeight", 1));
        for (var col = 0; col < headers.Length; col++)
        {
            row10.Add(StringCell($"{ColName(col + 1)}10", StyleTableHeader, headers[col]));
        }
        sheetData.Add(row10);

        // Rows 11..N: Movement Rows
        for (var idx = 0; idx < dataRowCount; idx++)
        {
            var r = firstDataRow + idx;
            var mov = movementRows[idx];
            var isEntry = mov.EntryUnits > 0m;
            var docLabel = FormatDocumentTag(mov.Document, mov.DocumentCode);

            string? unitCostFormula = null;
            if (!isEntry)
            {
                if (idx == 0 && initialUnits > 0m)
                    unitCostFormula = "+L8";
                else if (idx > 0)
                    unitCostFormula = $"+K{r - 1}";
            }

            var totalEntryFormula = $"+ROUND(E{r}*G{r},2)";
            var totalExitFormula = $"+ROUND(F{r}*G{r},2)";
            var balanceUnitsFormula = idx == 0
                ? $"+I8+E{r}-F{r}"
                : $"+J{r - 1}+E{r}-F{r}";
            var avgCostFormula = $"IF(J{r}=0,0,L{r}/J{r})";
            var kardexBalanceFormula = idx == 0
                ? $"+E8+H{r}-I{r}"
                : $"+L{r - 1}+H{r}-I{r}";

            var excelDateSerial = mov.Date.Date.ToOADate();

            var dataRowEl = new XElement(MainNs + "row",
                new XAttribute("r", r),
                new XAttribute("spans", "1:12"),
                StringCell($"A{r}", StyleTextLeft, mov.MovementType),
                RawNumberCell($"B{r}", StyleDateLeft, excelDateSerial),
                StringOrEmptyCell($"C{r}", StyleRecordDocCenter, mov.RecordNumber),
                StringOrEmptyCell($"D{r}", StyleRecordDocCenter, docLabel),
                isEntry
                    ? NumberCell($"E{r}", StyleMovementQtyCenter, mov.EntryUnits)
                    : EmptyCell($"E{r}", StyleMovementQtyCenter),
                !isEntry && mov.ExitUnits > 0m
                    ? NumberCell($"F{r}", StyleMovementQtyCenter, mov.ExitUnits)
                    : EmptyCell($"F{r}", StyleMovementQtyCenter),
                NumberCell($"G{r}", StyleUnitCost2, mov.UnitCost, unitCostFormula),
                NumberCell($"H{r}", StyleNumRight2, mov.TotalEntryCost, totalEntryFormula),
                NumberCell($"I{r}", StyleNumRight2, mov.TotalExitCost, totalExitFormula),
                NumberCell($"J{r}", StyleUnits3, mov.BalanceUnits, balanceUnitsFormula),
                NumberCell($"K{r}", StyleAccounting2, mov.AverageCost, avgCostFormula),
                NumberCell($"L{r}", StyleAccounting2, mov.KardexBalance, kardexBalanceFormula));

            sheetData.Add(dataRowEl);
        }

        // Summary / Totalizado Row
        var summary = product?.Summary ?? new KardexProductSummaryDto
        {
            FinalUnits = initialUnits,
            FinalAverageCost = initialAvgCost,
            FinalKardexBalance = initialKardex
        };

        var countFormula = dataRowCount > 0 ? $"COUNTA(D{firstDataRow}:D{lastDataRow})" : null;
        var sumEntryUnitsFormula = dataRowCount > 0 ? $"SUM(E{firstDataRow}:E{lastDataRow})" : null;
        var sumExitUnitsFormula = dataRowCount > 0 ? $"SUM(F{firstDataRow}:F{lastDataRow})" : null;
        var sumEntryCostFormula = dataRowCount > 0 ? $"SUM(H{firstDataRow}:H{lastDataRow})" : null;
        var sumExitCostFormula = dataRowCount > 0 ? $"SUM(I{firstDataRow}:I{lastDataRow})" : null;
        var finalUnitsFormula = $"+I8+E{totalRow}-F{totalRow}";
        var finalAvgRefFormula = dataRowCount > 0 ? $"+K{lastDataRow}" : "+L8";
        var finalKardexFormula = $"+E8+H{totalRow}-I{totalRow}";

        sheetData.Add(new XElement(MainNs + "row",
            new XAttribute("r", totalRow),
            new XAttribute("spans", "1:12"),
            new XAttribute("ht", "13.15"),
            new XAttribute("thickBot", 1),
            StringCell($"A{totalRow}", StyleTotalLabel, "Totalizado"),
            EmptyCell($"B{totalRow}", StyleTotalLabel),
            EmptyCell($"C{totalRow}", StyleTotalLabel),
            NumberCell($"D{totalRow}", StyleTotalIntCenter, summary.RowCount, countFormula),
            NumberCell($"E{totalRow}", StyleTotalIntCenter, summary.TotalEntries, sumEntryUnitsFormula),
            NumberCell($"F{totalRow}", StyleTotalIntCenter, summary.TotalExits, sumExitUnitsFormula),
            EmptyCell($"G{totalRow}", StyleTotalNumRight),
            NumberCell($"H{totalRow}", StyleTotalNumCenter, summary.TotalEntryCost, sumEntryCostFormula),
            NumberCell($"I{totalRow}", StyleTotalNumRight, summary.TotalExitCost, sumExitCostFormula),
            NumberCell($"J{totalRow}", StyleTotalIntRight, summary.FinalUnits, finalUnitsFormula),
            NumberCell($"K{totalRow}", StyleTotalNumRight, summary.FinalAverageCost, finalAvgRefFormula),
            NumberCell($"L{totalRow}", StyleTotalNumRight, summary.FinalKardexBalance, finalKardexFormula)));

        // Blank spacer before Final Summary Row
        sheetData.Add(new XElement(MainNs + "row",
            new XAttribute("r", blankBeforeFinalRow),
            new XAttribute("spans", "1:12"),
            new XAttribute("ht", "13.15"),
            new XAttribute("thickTop", 1)));

        // Final Summary Bar Row (mirrors Row 8)
        var finalKardexCellRefFormula = dataRowCount > 0 ? $"+L{lastDataRow}" : "+E8";
        var finalUnitsCellRefFormula = dataRowCount > 0 ? $"+J{lastDataRow}" : "+I8";
        var finalAvgCellRefFormula = dataRowCount > 0 ? $"+K{lastDataRow}" : "+L8";

        sheetData.Add(new XElement(MainNs + "row",
            new XAttribute("r", finalSummaryRow),
            new XAttribute("spans", "1:12"),
            new XAttribute("ht", "13.15"),
            StringCell($"A{finalSummaryRow}", StyleUmBox, unitLabel),
            EmptyCell($"B{finalSummaryRow}", StyleUmBox),
            StringCell($"C{finalSummaryRow}", StyleKardexLabelCenter, "Saldo Kardex final:"),
            EmptyCell($"D{finalSummaryRow}", StyleKardexLabelCenter),
            NumberCell($"E{finalSummaryRow}", StyleKardexCurrencyCenter, summary.FinalKardexBalance, finalKardexCellRefFormula),
            EmptyCell($"F{finalSummaryRow}", StyleKardexCurrencyCenter),
            StringCell($"G{finalSummaryRow}", StyleBalanceLabelRight, "Inventario Final:"),
            EmptyCell($"H{finalSummaryRow}", StyleBalanceLabelRight),
            NumberCell($"I{finalSummaryRow}", unitStyleIndex, summary.FinalUnits, finalUnitsCellRefFormula),
            StringCell($"J{finalSummaryRow}", StyleAvgCostLabelCenter, "Costo Promedio:"),
            EmptyCell($"K{finalSummaryRow}", StyleAvgCostLabelCenter),
            NumberCell($"L{finalSummaryRow}", StyleFinalCostCurrency, summary.FinalAverageCost, finalAvgCellRefFormula)));

        var mergeRefs = new[]
        {
            "A1:L1",
            "A2:L2",
            "A3:L3",
            "A4:L4",
            "A6:D6",
            "E6:L6",
            "A7:C7",
            "D7:G7",
            "H7:L7",
            "A8:B8",
            "C8:D8",
            "E8:F8",
            "G8:H8",
            "J8:K8",
            $"A{totalRow}:C{totalRow}",
            $"A{finalSummaryRow}:B{finalSummaryRow}",
            $"C{finalSummaryRow}:D{finalSummaryRow}",
            $"E{finalSummaryRow}:F{finalSummaryRow}",
            $"G{finalSummaryRow}:H{finalSummaryRow}",
            $"J{finalSummaryRow}:K{finalSummaryRow}"
        };

        var mergeCells = new XElement(MainNs + "mergeCells",
            new XAttribute("count", mergeRefs.Length),
            mergeRefs.Select(r => new XElement(MainNs + "mergeCell", new XAttribute("ref", r))));

        var cols = new XElement(MainNs + "cols",
            ColDef(1, 1, 11.5, StyleColCenterMiddle),
            ColDef(2, 2, 11.5, StyleColDefaultMiddle),
            ColDef(3, 3, 15.0, StyleColDefaultMiddle),
            ColDef(4, 4, 18.0, StyleColDefaultMiddle),
            ColDef(5, 6, 9.5, StyleColDefaultMiddle),
            ColDef(7, 7, 13.0, StyleColDefaultMiddle),
            ColDef(8, 8, 13.0, StyleColCenterMiddle),
            ColDef(9, 9, 13.0, StyleColDefaultMiddle),
            ColDef(10, 10, 12.0, StyleColDefaultMiddle),
            ColDef(11, 11, 14.0, StyleColDefaultMiddle),
            ColDef(12, 12, 14.0, StyleColDefaultMiddle));

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(MainNs + "worksheet",
                new XAttribute(XNamespace.Xmlns + "r", RelNs),
                new XElement(MainNs + "sheetPr",
                    new XElement(MainNs + "tabColor", new XAttribute("theme", 4))),
                new XElement(MainNs + "dimension",
                    new XAttribute("ref", $"A1:L{finalSummaryRow}")),
                new XElement(MainNs + "sheetViews",
                    new XElement(MainNs + "sheetView",
                        new XAttribute("showGridLines", 0),
                        new XAttribute("zoomScaleNormal", 100),
                        new XAttribute("workbookViewId", 0),
                        new XElement(MainNs + "pane",
                            new XAttribute("ySplit", 10),
                            new XAttribute("topLeftCell", "A11"),
                            new XAttribute("activePane", "bottomLeft"),
                            new XAttribute("state", "frozen")))),
                new XElement(MainNs + "sheetFormatPr",
                    new XAttribute("defaultColWidth", "10.7109375"),
                    new XAttribute("defaultRowHeight", "12.6")),
                cols,
                sheetData,
                mergeCells,
                new XElement(MainNs + "pageMargins",
                    new XAttribute("left", "0.7"),
                    new XAttribute("right", "0.7"),
                    new XAttribute("top", "0.75"),
                    new XAttribute("bottom", "0.75"),
                    new XAttribute("header", "0.3"),
                    new XAttribute("footer", "0.3")),
                new XElement(MainNs + "pageSetup",
                    new XAttribute("scale", "86"),
                    new XAttribute("orientation", "portrait"))));
    }

    private static XElement BuildMergedBannerRow(
        int rowNumber,
        double? height,
        int styleIndex,
        params (int StartCol, string Text)[] values)
    {
        var textByCol = values.ToDictionary(v => v.StartCol, v => v.Text);
        var row = new XElement(MainNs + "row",
            new XAttribute("r", rowNumber),
            new XAttribute("spans", "1:12"));

        if (height.HasValue)
        {
            row.Add(new XAttribute("ht", height.Value.ToString(CultureInfo.InvariantCulture)));
        }

        for (var col = 1; col <= 12; col++)
        {
            var cellRef = $"{ColName(col)}{rowNumber}";
            if (textByCol.TryGetValue(col, out var text))
                row.Add(StringCell(cellRef, styleIndex, text));
            else
                row.Add(EmptyCell(cellRef, styleIndex));
        }

        return row;
    }

    private static XElement ColDef(int min, int max, double width, int style) =>
        new(MainNs + "col",
            new XAttribute("min", min),
            new XAttribute("max", max),
            new XAttribute("width", width.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("style", style),
            new XAttribute("customWidth", 1));

    private static XElement EmptyCell(string cellRef, int styleIndex) =>
        new(MainNs + "c",
            new XAttribute("r", cellRef),
            new XAttribute("s", styleIndex));

    private static XElement StringOrEmptyCell(string cellRef, int styleIndex, string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? EmptyCell(cellRef, styleIndex)
            : StringCell(cellRef, styleIndex, text.Trim());

    private static XElement StringCell(string cellRef, int styleIndex, string text) =>
        new(MainNs + "c",
            new XAttribute("r", cellRef),
            new XAttribute("s", styleIndex),
            new XAttribute("t", "inlineStr"),
            new XElement(MainNs + "is",
                new XElement(MainNs + "t",
                    text.StartsWith(' ') || text.EndsWith(' ')
                        ? new XAttribute(XNamespace.Xml + "space", "preserve")
                        : null,
                    SanitizeXmlText(text))));

    private static XElement NumberCell(string cellRef, int styleIndex, decimal value, string? formula = null)
    {
        var cell = new XElement(MainNs + "c",
            new XAttribute("r", cellRef),
            new XAttribute("s", styleIndex));

        if (!string.IsNullOrEmpty(formula))
        {
            cell.Add(new XElement(MainNs + "f", formula));
        }

        cell.Add(new XElement(MainNs + "v", value.ToString("G29", CultureInfo.InvariantCulture)));
        return cell;
    }

    private static XElement RawNumberCell(string cellRef, int styleIndex, double value) =>
        new(MainNs + "c",
            new XAttribute("r", cellRef),
            new XAttribute("s", styleIndex),
            new XElement(MainNs + "v", value.ToString("R", CultureInfo.InvariantCulture)));

    private static string ColName(int colNumber)
    {
        var dividend = colNumber;
        var name = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            name = Convert.ToChar('A' + modulo) + name;
            dividend = (dividend - modulo) / 26;
        }
        return name;
    }

    private static string FormatPeriodRange(DateTime from, DateTime to) =>
        $"Del {from.Day}/{from.Month}/{from.Year} Al {to.Day}/{to.Month}/{to.Year}";

    private static string? FormatDocumentTag(ReferenceType reference, string? documentCode)
    {
        var prefix = reference switch
        {
            ReferenceType.FT => "FT#",
            ReferenceType.RQ => "RQ#",
            ReferenceType.BL => "BL#",
            _ => null
        };

        if (prefix is null)
            return null;

        return string.IsNullOrWhiteSpace(documentCode)
            ? prefix
            : $"{prefix} {documentCode.Trim()}";
    }

    private static string SanitizeFormatLiteral(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var cleaned = raw.Trim().Replace("\"", string.Empty).Replace("\\", string.Empty);
        return cleaned.Length > 8 ? cleaned[..8] : cleaned;
    }

    private static string SanitizeXmlText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch == '\t' || ch == '\n' || ch == '\r' || ch >= ' ')
                sb.Append(ch);
        }
        return sb.ToString();
    }

    private static string BuildUniqueSheetName(string? productCode, int index, HashSet<string> usedNames)
    {
        var candidate = string.IsNullOrWhiteSpace(productCode)
            ? $"KARDEX {index}"
            : SanitizeSheetName(productCode.Trim());

        if (string.IsNullOrWhiteSpace(candidate))
            candidate = $"KARDEX {index}";

        if (candidate.Length > 31)
            candidate = candidate[..31].Trim();

        var baseName = candidate;
        var suffix = 2;
        while (!usedNames.Add(candidate))
        {
            var tag = $" ({suffix++})";
            var maxBaseLen = 31 - tag.Length;
            candidate = (baseName.Length > maxBaseLen ? baseName[..maxBaseLen] : baseName) + tag;
        }

        return candidate;
    }

    private static string SanitizeSheetName(string input)
    {
        ReadOnlySpan<char> invalid = ['\\', '/', '?', '*', '[', ']', ':'];
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            sb.Append(invalid.Contains(ch) ? '-' : ch);
        }
        return sb.ToString().Trim('\'');
    }

    private static void WriteXmlEntry(ZipArchive archive, string entryPath, XDocument document)
    {
        var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        document.Save(writer, SaveOptions.DisableFormatting);
    }

    private const string BaseStylesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <numFmts count="8">
            <numFmt numFmtId="43" formatCode="_-* #,##0.00_-;\-* #,##0.00_-;_-* &quot;-&quot;??_-;_-@_-"/>
            <numFmt numFmtId="164" formatCode="_-&quot;C$&quot;* #,##0.00_-;\-&quot;C$&quot;* #,##0.00_-;_-&quot;C$&quot;* &quot;-&quot;??_-;_-@_-"/>
            <numFmt numFmtId="165" formatCode="0.000&quot;Gl&quot;"/>
            <numFmt numFmtId="166" formatCode="&quot;C$&quot;#,##0.00"/>
            <numFmt numFmtId="167" formatCode="#,##0.00_);\-#,##0.00"/>
            <numFmt numFmtId="168" formatCode="#,##0.000_ ;\-#,##0.000\ "/>
            <numFmt numFmtId="169" formatCode="#,##0.00_);\(#,##0.00\)"/>
            <numFmt numFmtId="170" formatCode="dd&quot;/&quot;mm&quot;/&quot;yyyy"/>
          </numFmts>
          <fonts count="13">
            <font><sz val="10"/><color indexed="8"/><name val="MS Sans Serif"/></font>
            <font><sz val="10"/><color indexed="8"/><name val="MS Sans Serif"/></font>
            <font><b/><sz val="8"/><color rgb="FF0070C0"/><name val="Times New Roman"/><family val="1"/></font>
            <font><b/><sz val="10"/><color rgb="FF0070C0"/><name val="Times New Roman"/><family val="1"/></font>
            <font><b/><sz val="9.85"/><color rgb="FF0070C0"/><name val="Times New Roman"/><family val="1"/></font>
            <font><sz val="9.85"/><color indexed="8"/><name val="Times New Roman"/><family val="1"/></font>
            <font><b/><sz val="9.85"/><name val="Times New Roman"/><family val="1"/></font>
            <font><b/><sz val="9.85"/><color indexed="8"/><name val="Times New Roman"/><family val="1"/></font>
            <font><b/><sz val="9.85"/><color theme="0"/><name val="Times New Roman"/><family val="1"/></font>
            <font><sz val="12"/><color indexed="8"/><name val="Times New Roman"/><family val="1"/></font>
            <font><b/><sz val="14.05"/><color indexed="8"/><name val="Times New Roman"/><family val="1"/></font>
            <font><sz val="9"/><color indexed="81"/><name val="Tahoma"/><family val="2"/></font>
            <font><b/><sz val="9"/><color indexed="81"/><name val="Tahoma"/><family val="2"/></font>
          </fonts>
          <fills count="6">
            <fill><patternFill patternType="none"/></fill>
            <fill><patternFill patternType="gray125"/></fill>
            <fill><patternFill patternType="solid"><fgColor theme="2"/><bgColor indexed="64"/></patternFill></fill>
            <fill><patternFill patternType="solid"><fgColor theme="4" tint="0.79998168889431442"/><bgColor indexed="64"/></patternFill></fill>
            <fill><patternFill patternType="solid"><fgColor theme="7" tint="0.79998168889431442"/><bgColor indexed="64"/></patternFill></fill>
            <fill><patternFill patternType="solid"><fgColor theme="4"/><bgColor indexed="64"/></patternFill></fill>
          </fills>
          <borders count="4">
            <border><left/><right/><top/><bottom/><diagonal/></border>
            <border><left/><right/><top style="thin"><color indexed="8"/></top><bottom style="double"><color indexed="64"/></bottom><diagonal/></border>
            <border><left/><right/><top style="thin"><color indexed="64"/></top><bottom style="thin"><color indexed="64"/></bottom><diagonal/></border>
            <border><left/><right/><top style="thin"><color rgb="FF00B0F0"/></top><bottom style="thin"><color rgb="FF00B0F0"/></bottom><diagonal/></border>
          </borders>
          <cellStyleXfs count="2">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0"/>
            <xf numFmtId="43" fontId="1" fillId="0" borderId="0" applyFont="0" applyFill="0" applyBorder="0" applyAlignment="0" applyProtection="0"/>
          </cellStyleXfs>
          <cellXfs count="35">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="4" fontId="5" fillId="0" borderId="0" xfId="0" applyNumberFormat="1" applyFont="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>
            <xf numFmtId="167" fontId="5" fillId="0" borderId="0" xfId="0" applyNumberFormat="1" applyFont="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>
            <xf numFmtId="0" fontId="5" fillId="0" borderId="0" xfId="0" applyFont="1" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="5" fillId="0" borderId="0" xfId="0" applyFont="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="167" fontId="6" fillId="3" borderId="1" xfId="0" applyNumberFormat="1" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>
            <xf numFmtId="0" fontId="6" fillId="3" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>
            <xf numFmtId="167" fontId="6" fillId="3" borderId="1" xfId="0" applyNumberFormat="1" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="0" fontId="6" fillId="3" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="43" fontId="5" fillId="0" borderId="0" xfId="1" applyFont="1" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="168" fontId="5" fillId="0" borderId="0" xfId="0" applyNumberFormat="1" applyFont="1" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="169" fontId="5" fillId="0" borderId="0" xfId="0" applyNumberFormat="1" applyFont="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>
            <xf numFmtId="0" fontId="5" fillId="0" borderId="0" xfId="1" applyNumberFormat="1" applyFont="1" applyAlignment="1"><alignment horizontal="center"/></xf>
            <xf numFmtId="170" fontId="5" fillId="0" borderId="0" xfId="0" applyNumberFormat="1" applyFont="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="170" fontId="5" fillId="0" borderId="0" xfId="0" applyNumberFormat="1" applyFont="1" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
            <xf numFmtId="0" fontId="7" fillId="0" borderId="0" xfId="0" applyFont="1" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="7" fillId="0" borderId="0" xfId="0" applyFont="1" applyAlignment="1"><alignment horizontal="left" vertical="center"/></xf>
            <xf numFmtId="165" fontId="2" fillId="2" borderId="3" xfId="0" applyNumberFormat="1" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="166" fontId="2" fillId="2" borderId="3" xfId="1" applyNumberFormat="1" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="164" fontId="2" fillId="2" borderId="3" xfId="1" applyNumberFormat="1" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="7" fillId="0" borderId="2" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
            <xf numFmtId="0" fontId="8" fillId="5" borderId="0" xfId="0" applyFont="1" applyFill="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="167" fontId="6" fillId="3" borderId="1" xfId="0" applyNumberFormat="1" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="3" fontId="2" fillId="2" borderId="3" xfId="0" applyNumberFormat="1" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>
            <xf numFmtId="0" fontId="3" fillId="2" borderId="3" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="3" fontId="2" fillId="2" borderId="3" xfId="0" applyNumberFormat="1" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="0" fontId="4" fillId="2" borderId="3" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="166" fontId="2" fillId="2" borderId="3" xfId="0" applyNumberFormat="1" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="0" fontId="5" fillId="0" borderId="0" xfId="0" applyFont="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="0" fontId="9" fillId="0" borderId="0" xfId="0" applyFont="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="0" fontId="10" fillId="0" borderId="0" xfId="0" applyFont="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
            <xf numFmtId="0" fontId="5" fillId="4" borderId="0" xfId="0" applyFont="1" applyFill="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf>
          </cellXfs>
          <cellStyles count="2">
            <cellStyle name="Millares" xfId="1" builtinId="3"/>
            <cellStyle name="Normal" xfId="0" builtinId="0"/>
          </cellStyles>
          <dxfs count="0"/>
          <tableStyles count="0" defaultTableStyle="TableStyleMedium2" defaultPivotStyle="PivotStyleLight16"/>
        </styleSheet>
        """;

    private const string OfficeThemeXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="Tema de Office">
          <a:themeElements>
            <a:clrScheme name="Office">
              <a:dk1><a:sysClr val="windowText" lastClr="000000"/></a:dk1>
              <a:lt1><a:sysClr val="window" lastClr="FFFFFF"/></a:lt1>
              <a:dk2><a:srgbClr val="0E2841"/></a:dk2>
              <a:lt2><a:srgbClr val="E8E8E8"/></a:lt2>
              <a:accent1><a:srgbClr val="156082"/></a:accent1>
              <a:accent2><a:srgbClr val="E97132"/></a:accent2>
              <a:accent3><a:srgbClr val="196B24"/></a:accent3>
              <a:accent4><a:srgbClr val="0F9ED5"/></a:accent4>
              <a:accent5><a:srgbClr val="A02B93"/></a:accent5>
              <a:accent6><a:srgbClr val="4EA72E"/></a:accent6>
              <a:hlink><a:srgbClr val="467886"/></a:hlink>
              <a:folHlink><a:srgbClr val="96607D"/></a:folHlink>
            </a:clrScheme>
            <a:fontScheme name="Office">
              <a:majorFont>
                <a:latin typeface="Aptos Display"/>
                <a:ea typeface=""/>
                <a:cs typeface=""/>
              </a:majorFont>
              <a:minorFont>
                <a:latin typeface="Aptos Narrow"/>
                <a:ea typeface=""/>
                <a:cs typeface=""/>
              </a:minorFont>
            </a:fontScheme>
            <a:fmtScheme name="Office">
              <a:fillStyleLst>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
              </a:fillStyleLst>
              <a:lnStyleLst>
                <a:ln w="12700" cap="flat" cmpd="sng" algn="ctr"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:prstDash val="solid"/><a:miter lim="800000"/></a:ln>
                <a:ln w="19050" cap="flat" cmpd="sng" algn="ctr"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:prstDash val="solid"/><a:miter lim="800000"/></a:ln>
                <a:ln w="25400" cap="flat" cmpd="sng" algn="ctr"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:prstDash val="solid"/><a:miter lim="800000"/></a:ln>
              </a:lnStyleLst>
              <a:effectStyleLst>
                <a:effectStyle><a:effectLst/></a:effectStyle>
                <a:effectStyle><a:effectLst/></a:effectStyle>
                <a:effectStyle><a:effectLst/></a:effectStyle>
              </a:effectStyleLst>
              <a:bgFillStyleLst>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
              </a:bgFillStyleLst>
            </a:fmtScheme>
          </a:themeElements>
          <a:objectDefaults/>
          <a:extraClrSchemeLst/>
        </a:theme>
        """;
}
