namespace Planbord.Core;

/// One assigned project's planned load: a fixed share of the week while it runs.
/// Verlengd = past its end date but still planned, so it keeps its load.
public record ProjectLast(DateOnly? Start, DateOnly? Einde, int Procent, bool Verlengd = false)
{
    public bool LooptOp(DateOnly d) => (Start is null || Start <= d) && (Verlengd || Einde is null || Einde >= d);
}

public record PrognoseResultaat(List<Maand> Maanden, List<Project> Verlopen);

public record Maand(DateOnly Begin, double Capaciteit, double Last)
{
    public double Vrij => Capaciteit - Last;
    /// A month without working days counts as fully free.
    public double VrijAandeel => Capaciteit == 0 ? 1 : Vrij / Capaciteit;
    public bool Overboekt => Last > Capaciteit + 1e-9;
}

/// Monthly availability forecast against a fixed 38-hour week.
public static class Prognose
{
    public const double UrenPerDag = 7.6;
    /// Typical duration in months for the "available from" check per category (1 year+ checked over 12 months).
    public static readonly int[] TypischeDuur = [1, 3, 6, 12, 12];

    /// Per month from 'vanaf' (the first month starts at 'vanaf' itself): capacity = working days x 7.6 h,
    /// load = per working day the sum of running projects' shares x 7.6 h. Absent days count as 0 for both.
    // ponytail: public holidays ignored on purpose (out of scope per the notes)
    public static List<Maand> PerMaand(DateOnly vanaf, int aantal, IReadOnlyCollection<ProjectLast> projecten,
        IReadOnlyCollection<(DateOnly Van, DateOnly Tot)> afwezig)
    {
        var maanden = new List<Maand>();
        var eerste = new DateOnly(vanaf.Year, vanaf.Month, 1);
        for (var m = 0; m < aantal; m++)
        {
            var begin = eerste.AddMonths(m);
            double capaciteit = 0, last = 0;
            for (var d = m == 0 ? vanaf : begin; d < begin.AddMonths(1); d = d.AddDays(1))
            {
                if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                if (afwezig.Any(a => a.Van <= d && a.Tot >= d)) continue;
                capaciteit += UrenPerDag;
                last += projecten.Where(p => p.LooptOp(d)).Sum(p => p.Procent) / 100.0 * UrenPerDag;
            }
            maanden.Add(new Maand(begin, capaciteit, last));
        }
        return maanden;
    }

    /// Index of the first month from which the free share stays at least 'procent' for 'duurMaanden'
    /// consecutive months, or null when that never happens within the given months.
    public static int? BeschikbaarVanaf(IReadOnlyList<Maand> maanden, int procent, int duurMaanden)
    {
        for (var s = 0; s + duurMaanden <= maanden.Count; s++)
            if (Enumerable.Range(s, duurMaanden).All(i => maanden[i].VrijAandeel >= procent / 100.0 - 1e-9)) return s;
        return null;
    }

    public static double VrijeUren(IReadOnlyList<Maand> maanden, int start, int duurMaanden) =>
        maanden.Skip(start).Take(duurMaanden).Sum(m => m.Vrij);
}
