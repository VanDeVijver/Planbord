using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Planbord.Web;

/// Semicolon CSV with BOM so Excel (nl-BE) opens it correctly.
public static class Csv
{
    public static string Rij(IEnumerable<string?> velden) =>
        string.Join(';', velden.Select(v => v is null ? "" : v.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? $"\"{v.Replace("\"", "\"\"")}\"" : v));

    public static FileContentResult Bestand(StringBuilder sb, string naam) =>
        new(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(), "text/csv; charset=utf-8") { FileDownloadName = naam };
}
