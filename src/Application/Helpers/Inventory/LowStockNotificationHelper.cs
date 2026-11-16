using System.Net;
using System.Text;
using System.Globalization;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Inventory;

public static class LowStockNotificationHelper
{
    public static string BuildEmailBody(
        IEnumerable<(string Name, string Code, decimal Available, decimal Minimum)> items,
        string referenceCode,
        DateTime createdAt)
    {
        const string thStyle =
            "padding:10px 12px;font-size:13px;font-weight:bold;color:#52525b;border-bottom:1px solid #d4d4d8;";
        const string tdStyle =
            "padding:12px;font-size:14px;color:#18181b;border-bottom:1px solid #e4e4e7;";

        var rows = new StringBuilder();
        foreach (var item in items)
        {
            var missing = Math.Max(0m, item.Minimum - item.Available);

            rows.Append("<tr>");
            rows.Append("<td style=\"" + tdStyle + "\">" + Enc(item.Name) + "</td>");
            rows.Append("<td style=\"" + tdStyle + "color:#52525b;\">" + Enc(item.Code) + "</td>");
            rows.Append("<td align=\"right\" style=\"" + tdStyle + "font-weight:bold;color:#b91c1c;\">" + Num(item.Available) + "</td>");
            rows.Append("<td align=\"right\" style=\"" + tdStyle + "\">" + Num(item.Minimum) + "</td>");
            rows.Append("<td align=\"right\" bgcolor=\"#fee2e2\" style=\"" + tdStyle + "font-weight:bold;color:#b91c1c;background-color:#fee2e2;\">" + Num(missing) + "</td>");
            rows.Append("</tr>");
        }

        var body = "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"#f4f4f5\" style=\"background-color:#f4f4f5;\">";
        body += "<tr><td align=\"center\" style=\"padding:24px 12px;\">";
        body += "<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"#ffffff\" style=\"width:100%;max-width:600px;background-color:#ffffff;border:1px solid #e4e4e7;font-family:Segoe UI,Arial,sans-serif;\">";
        body += "<tr><td bgcolor=\"#fef3c7\" style=\"background-color:#fef3c7;padding:14px 20px;font-size:16px;font-weight:bold;color:#92400e;\">⚠️ Alerta de stock bajo</td></tr>";
        body += "<tr><td style=\"padding:20px;\">";
        body += "<p style=\"margin:0 0 4px 0;font-size:15px;line-height:1.6;color:#18181b;\">Se detecto stock por debajo del minimo tras procesar <b>" + Enc(referenceCode) + "</b>.</p>";
        body += "<p style=\"margin:0 0 18px 0;font-size:13px;color:#71717a;\">Procesado el " + createdAt.ToString("dd/MM/yyyy") + " a las " + createdAt.ToString("HH:mm") + "</p>";
        body += "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse;\">";
        body += "<thead><tr bgcolor=\"#f4f4f5\" style=\"background-color:#f4f4f5;\">";
        body += "<th align=\"left\" style=\"" + thStyle + "\">Producto</th>";
        body += "<th align=\"left\" style=\"" + thStyle + "\">Codigo</th>";
        body += "<th align=\"right\" style=\"" + thStyle + "\">Restante</th>";
        body += "<th align=\"right\" style=\"" + thStyle + "\">Minimo</th>";
        body += "<th align=\"right\" style=\"" + thStyle + "\">Faltante</th>";
        body += "</tr></thead>";
        body += "<tbody>" + rows.ToString() + "</tbody>";
        body += "</table></td></tr></table></td></tr></table>";

        return body;

        static string Enc(string value) => WebUtility.HtmlEncode(value);
        static string Num(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
    }

    public static string BuildPushBody(
        IEnumerable<(string Name, string Code, decimal Available, decimal Minimum)> items,
        string referenceCode)
    {
        var parts = items.Select(i => $"{i.Name} ({i.Code}): {i.Available:N2}/{i.Minimum:N2}");
        return $"Stock bajo tras {referenceCode}: {string.Join("; ", parts)}";
    }
}
