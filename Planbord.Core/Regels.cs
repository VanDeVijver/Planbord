using System.Globalization;
using System.Text;

namespace Planbord.Core;

public static class PlanningRegels
{
    /// First assignment of an employee's day is normal; every later one is an intervention.
    public static void Herbereken(IEnumerable<Planning> rijenVanEenDag)
    {
        var eerste = true;
        foreach (var p in rijenVanEenDag.OrderBy(p => p.ToegewezenOp).ThenBy(p => p.Id))
        {
            p.IsInterventie = !eerste;
            if (eerste) p.Goedgekeurd = false;
            eerste = false;
        }
    }

    public static DateOnly Maandag(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
}

public enum ProjectStatus { Actief, RecentAfgerond }

public static class ProjectRegels
{
    /// Length category 1–5 from the planned duration: the first limit (1, 3, 6, 12 months)
    /// the project does not exceed. Unknown start or end date counts as "1 year+".
    public static int Categorie(DateOnly? start, DateOnly? einde)
    {
        if (start is not { } s || einde is not { } e) return 5;
        int[] maanden = [1, 3, 6, 12];
        for (var i = 0; i < maanden.Length; i++)
            if (e <= s.AddMonths(maanden[i])) return i + 1;
        return 5;
    }

    /// Active, finished within the last 3 months, or null (not started yet / finished longer ago).
    /// A missing start date counts as started.
    public static ProjectStatus? Status(DateOnly? start, DateOnly? einde, DateOnly vandaag)
    {
        if (start > vandaag) return null;
        if (einde is null || einde >= vandaag) return ProjectStatus.Actief;
        return einde >= vandaag.AddMonths(-3) ? ProjectStatus.RecentAfgerond : null;
    }

    public static string Geleden(DateOnly datum, DateOnly vandaag)
    {
        var dagen = vandaag.DayNumber - datum.DayNumber;
        return dagen switch
        {
            0 => "vandaag",
            1 => "gisteren",
            < 14 => $"{dagen} dagen geleden",
            < 60 => $"{dagen / 7} weken geleden",
            _ => $"{dagen / 30} maanden geleden",
        };
    }
}

public static class Tijdlijn
{
    /// Left offset and width as fractions (0–1) of the window [van, tot) for a bar from start to einde
    /// (both inclusive), clipped to the window. No start date = from the left edge, no end date = to the
    /// right edge. Null when the bar falls outside the window.
    public static (double Links, double Breedte)? Positie(DateOnly van, DateOnly tot, DateOnly? start, DateOnly? einde)
    {
        double totaal = tot.DayNumber - van.DayNumber;
        var s = Math.Max((start ?? van).DayNumber, van.DayNumber);
        var e = Math.Min(einde is { } x ? x.DayNumber + 1 : tot.DayNumber, tot.DayNumber);
        return e <= s ? null : ((s - van.DayNumber) / totaal, (e - s) / totaal);
    }
}

public class RapportInvoer
{
    public TimeOnly? Start { get; set; }
    public TimeOnly? Einde { get; set; }
    public int PauzeMinuten { get; set; }
    public List<RegelInvoer> Regels { get; set; } = [];
}

public class RegelInvoer
{
    public int ProjectId { get; set; }
    public string ProjectNaam { get; set; } = "";
    public RegelStatus? Status { get; set; }
    public string? Omschrijving { get; set; }
    public int? Minuten { get; set; }
}

public static class RapportRegels
{
    public static int? NettoMinuten(TimeOnly? start, TimeOnly? einde, int pauze) =>
        start is { } s && einde is { } e ? (int)(e - s).TotalMinutes - pauze : null;

    /// Normalises the input (zero hours for exceptions, auto-allocation for one worked site)
    /// and returns the errors. Drafts are only normalised; submission is fully checked.
    public static List<string> Valideer(RapportInvoer r, bool indienen)
    {
        var fouten = new List<string>();
        foreach (var regel in r.Regels.Where(x => x.Status is RegelStatus.NIET_GEWEEST or RegelStatus.GEANNULEERD))
            regel.Minuten = 0;
        var gewerkt = r.Regels.Where(x => x.Status == RegelStatus.GEWERKT).ToList();
        var netto = NettoMinuten(r.Start, r.Einde, r.PauzeMinuten);
        if (gewerkt.Count == 1 && netto > 0) gewerkt[0].Minuten = netto;
        if (!indienen) return fouten;

        if (r.Regels.Count == 0) fouten.Add("Er is geen planning voor deze dag.");
        foreach (var regel in r.Regels)
        {
            if (regel.Status is null) fouten.Add($"Kies een status voor {regel.ProjectNaam}.");
            else if (string.IsNullOrWhiteSpace(regel.Omschrijving))
                fouten.Add(regel.Status == RegelStatus.GEWERKT
                    ? $"Beschrijf het uitgevoerde werk voor {regel.ProjectNaam}."
                    : $"Geef een reden op voor {regel.ProjectNaam}.");
        }
        if (gewerkt.Count == 0) return fouten;

        // ponytail: no overnight shifts (end must be after start on the same day)
        if (r.Start is null || r.Einde is null) { fouten.Add("Vul begin- en einduur in."); return fouten; }
        if (r.Einde <= r.Start) { fouten.Add("Het einduur moet na het beginuur liggen."); return fouten; }
        if (r.PauzeMinuten < 0) { fouten.Add("De pauze kan niet negatief zijn."); return fouten; }
        if (netto <= 0) { fouten.Add("De pauze is langer dan de werkdag."); return fouten; }
        if (gewerkt.Count > 1)
        {
            if (gewerkt.Any(x => x.Minuten is null or <= 0))
                fouten.Add("Verdeel de werktijd over elke werf waar je gewerkt hebt.");
            else if (gewerkt.Sum(x => x.Minuten!.Value) != netto)
                fouten.Add($"De verdeling ({Duur.Formatteer(gewerkt.Sum(x => x.Minuten!.Value))}) moet exact gelijk zijn aan de netto werktijd ({Duur.Formatteer(netto!.Value)}).");
        }
        return fouten;
    }
}

public static class Duur
{
    /// Parses "7:30", "7" or "7,5"/"7.5" (hours) into minutes.
    public static int? Parse(string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst)) return null;
        tekst = tekst.Trim();
        var delen = tekst.Split(':');
        if (delen.Length == 2 && int.TryParse(delen[0], out var u) && int.TryParse(delen[1], out var m) && u >= 0 && m is >= 0 and < 60)
            return u * 60 + m;
        if (delen.Length == 1 && decimal.TryParse(tekst.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var uren) && uren >= 0)
            return (int)Math.Round(uren * 60);
        return null;
    }

    public static string Formatteer(int minuten) => $"{minuten / 60}:{minuten % 60:00}";
}

public static class BerichtTekst
{
    public static string Samenvatting(DateOnly datum, IEnumerable<Project> projecten)
    {
        var sb = new StringBuilder();
        var lijst = projecten.ToList();
        sb.AppendLine($"Planning voor {datum.ToString("dddd d MMMM", new CultureInfo("nl-BE"))}:");
        if (lijst.Count == 0) sb.AppendLine("- Geen werven gepland.");
        foreach (var p in lijst)
        {
            sb.AppendLine($"- {p.Naam}{(string.IsNullOrEmpty(p.Adres) ? "" : $" ({p.Adres})")}");
            foreach (var t in p.Taken.Where(t => !t.Klaar)) sb.AppendLine($"    • {t.Omschrijving}");
        }
        return sb.ToString().TrimEnd();
    }
}
