using Microsoft.EntityFrameworkCore;

namespace Planbord.Core;

public static class DemoData
{
    const int Dagminuten = 456; // 38-hour week = 7:36 a day (07:00–15:06, 30 min break)
    static readonly DateOnly HistoriekVanaf = new(2025, 1, 1);

    static readonly string[] Omschrijvingen =
    [
        "Kabels getrokken", "Verdeelbord bekabeld", "Stopcontacten geplaatst", "Verlichting gemonteerd",
        "Kabelgoten geplaatst", "Metingen en keuring voorbereid", "Leidingen ingefreesd", "Aansluitingen getest",
        "Materiaal klaargezet en opgeruimd", "Storing opgespoord en hersteld",
    ];

    /// One project leader's projects, back to back: (name, address, length in days, gap in days before the next).
    record Keten(int Leider, int StartOffset, (string Naam, string Adres, int Dagen, int Pauze)[] Projecten);

    // Five project leaders, each with one project at a time (yearly calendar), spread from early 2025 to 2028.
    static readonly Keten[] Ketens =
    [
        new(0, -650, [
            ("Renovatie Stadhuis", "Stadhuisplein 1, Lier", 240, 14),
            ("Keuken Restaurant Ter Dijk", "Dijkstraat 2, Mechelen", 150, 10),
            ("Ziekenhuis Campus Noord", "Noordring 100, Antwerpen", 400, 30),
            ("Appartementen Noord", "Noorderlaan 77, Antwerpen", 300, 0)]),
        new(1, -600, [
            ("Winkelcentrum Ring", "Ringlaan 50, Mechelen", 330, 20),
            ("Kantoor Zuidpark", "Zuidpark 1, Gent", 120, 15),
            ("Dakwerken Vrijthof", "Vrijthof 5, Mechelen", 95, 65),
            ("Sanitair Rusthuis Ter Linden", "Lindestraat 20, Lier", 120, 10),
            ("Loods Haven", "Havenweg 2, Antwerpen", 270, 0)]),
        new(2, -560, [
            ("Herstelling Brouwerij", "Brouwerijstraat 6, Lier", 25, 10),
            ("Uitbreiding Fabriek Noord", "Fabriekstraat 3, Antwerpen", 420, 14),
            ("Zonnepanelen Industriepark", "Industriepark 14, Duffel", 300, 20),
            ("Brandmelding Bibliotheek", "Bibliotheekplein 2, Mechelen", 150, 0)]),
        new(3, -500, [
            ("Verbouwing Bakkerij Smets", "Bruul 18, Mechelen", 60, 20),
            ("Schoolcampus West", "Westlaan 40, Mechelen", 330, 10),
            ("Laadpalen Gemeentehuis", "Grote Markt 1, Duffel", 65, 5),
            ("Onderhoud School Ter Beke", "Bekestraat 30, Duffel", 22, 7),
            ("Verlichting Parking Oost", "Oostweg 3, Mechelen", 70, 30),
            ("Nieuwbouw De Linde", "Lindelaan 4, Lier", 420, 0)]),
        new(4, -480, [
            ("Renovatie Turnzaal", "Sportlaan 11, Duffel", 110, 30),
            ("Noodherstelling Hangar 3", "Luchthavenlaan 3, Duffel", 15, 10),
            ("Bekabeling Kinderdagverblijf", "Speelpleinstraat 8, Lier", 70, 20),
            ("Keuring Sporthal De Nete", "Netelaan 9, Lier", 20, 180),
            ("Herstelling Stationsplein", "Stationsplein 1, Mechelen", 21, 39),
            ("Spoedklus Sint-Romboutscollege", "Sint-Romboutskerkhof 4, Mechelen", 24, 13),
            ("Kantoortoren Mechelen-Noord", "Nekkerspoel 2, Mechelen", 540, 0)]),
    ];

    /// Destructive: wipes all planning data (not user accounts) and seeds project leaders with their
    /// projects, crews, holidays, a planned current week and closed reports for every workday since 2025.
    public static async Task Reset(PlanbordDb db, DateOnly vandaag)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.DagrapportRegels.ExecuteDeleteAsync();
        await db.Dagrapporten.ExecuteDeleteAsync();
        await db.Berichten.ExecuteDeleteAsync();
        await db.Planningen.ExecuteDeleteAsync();
        await db.Afwezigheden.ExecuteDeleteAsync();
        await db.Taken.ExecuteDeleteAsync();
        await db.Projecten.ExecuteDeleteAsync();
        await db.Users.ExecuteUpdateAsync(u => u.SetProperty(x => x.MedewerkerId, (int?)null));
        await db.Medewerkers.ExecuteDeleteAsync();

        // 0 Jan, 1 Sofie, 2 Tom, 3 Lotte, 4 Kevin (project leaders), 5 Ines
        var mensen = new[] { "Jan Peeters", "Sofie Janssens", "Tom Maes", "Lotte Claes", "Kevin Wouters", "Ines Goossens" }
            .Select((n, i) => new Medewerker { Naam = n, IsProjectleider = i < 5 }).ToList();

        // Crew = the leader plus two colleagues, so everyone works on two or three chains.
        var ploegen = new Dictionary<Project, int[]>();
        var werven = new List<Project>();
        foreach (var k in Ketens)
        {
            var start = vandaag.AddDays(k.StartOffset);
            foreach (var (naam, adres, dagen, pauze) in k.Projecten)
            {
                var p = new Project { Naam = naam, Adres = adres, Startdatum = start, Einddatum = start.AddDays(dagen - 1), Projectleider = mensen[k.Leider] };
                ploegen[p] = [k.Leider, (k.Leider + 2) % 6, (k.Leider + 4) % 6];
                werven.Add(p);
                start = start.AddDays(dagen + pauze);
            }
        }
        Project Werf(string naam) => werven.Single(p => p.Naam == naam);
        Werf("Onderhoud School Ter Beke").Taken = [new() { Omschrijving = "Stelling afbreken" }, new() { Omschrijving = "Kabelgoten plaatsen" }];
        Werf("Nieuwbouw De Linde").Taken = [new() { Omschrijving = "Verdeelbord aansluiten" }];
        db.AddRange(mensen);
        db.AddRange(werven);
        await db.SaveChangesAsync();

        // Holidays per employee: two summer weeks (staggered) and the Christmas week, also future ones for the forecast.
        var verlof = new List<Afwezigheid>();
        for (var i = 0; i < mensen.Count; i++)
        {
            foreach (var jaar in new[] { 2025, 2026, 2027 })
            {
                var zomer = PlanningRegels.Maandag(new DateOnly(jaar, 7, 7).AddDays(7 * i));
                verlof.Add(new Afwezigheid { MedewerkerId = mensen[i].Id, Van = zomer, Tot = zomer.AddDays(11), Reden = "Zomerverlof" });
            }
            foreach (var jaar in new[] { 2025, 2026 })
            {
                var kerst = PlanningRegels.Maandag(new DateOnly(jaar, 12, 28));
                verlof.Add(new Afwezigheid { MedewerkerId = mensen[i].Id, Van = kerst, Tot = kerst.AddDays(4), Reden = "Kerstverlof" });
            }
        }
        db.Afwezigheden.AddRange(verlof);

        var maandag = PlanningRegels.Maandag(vandaag);
        var t = DateTime.UtcNow;
        var rnd = new Random(42);
        // Each day goes to the least-used running project of the employee's crews (or any running one).
        var gebruikt = new Dictionary<(int, Project), int>();
        List<Project> Lopend(DateOnly d) => werven.Where(p => p.Startdatum <= d && p.Einddatum >= d).ToList();
        List<Project> Kandidaten(int i, DateOnly d)
        {
            var lopend = Lopend(d);
            var eigen = lopend.Where(p => ploegen[p].Contains(i)).ToList();
            return eigen.Count > 0 ? eigen : lopend;
        }
        Project Kies(int i, List<Project> kandidaten, Project? niet = null)
        {
            var p = kandidaten.Where(x => x != niet).MinBy(x => gebruikt.GetValueOrDefault((i, x)))!;
            gebruikt[(i, p)] = gebruikt.GetValueOrDefault((i, p)) + 1;
            return p;
        }

        // History: every workday up to last week, 7:36 a day, closed (older) or submitted (last week) reports.
        for (var d = HistoriekVanaf; d < maandag; d = d.AddDays(1))
        {
            if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            for (var i = 0; i < mensen.Count; i++)
            {
                if (verlof.Any(a => a.MedewerkerId == mensen[i].Id && a.Van <= d && a.Tot >= d)) continue;
                var kandidaten = Kandidaten(i, d);
                if (kandidaten.Count == 0) continue;
                var hoofd = Kies(i, kandidaten);
                db.Planningen.Add(new Planning { MedewerkerId = mensen[i].Id, ProjectId = hoofd.Id, Datum = d, ToegewezenOp = t });
                var regels = new List<DagrapportRegel>();
                // About one day in six is split over two sites (an approved intervention).
                if (kandidaten.Count > 1 && rnd.Next(6) == 0)
                {
                    var tweede = Kies(i, kandidaten, hoofd);
                    db.Planningen.Add(new Planning { MedewerkerId = mensen[i].Id, ProjectId = tweede.Id, Datum = d, ToegewezenOp = t.AddMinutes(1), IsInterventie = true, Goedgekeurd = true });
                    regels.Add(Regel(hoofd, 240, rnd));
                    regels.Add(Regel(tweede, Dagminuten - 240, rnd));
                }
                else regels.Add(Regel(hoofd, Dagminuten, rnd));

                var gesloten = d < maandag.AddDays(-7);
                db.Dagrapporten.Add(new Dagrapport
                {
                    MedewerkerId = mensen[i].Id, Datum = d, Status = gesloten ? RapportStatus.Gesloten : RapportStatus.Ingediend,
                    Start = new TimeOnly(7, 0), Einde = new TimeOnly(15, 6), PauzeMinuten = 30,
                    IngediendOp = t, GeslotenOp = gesloten ? t : null, Regels = regels,
                });
            }
        }

        // Current week: planned on the crews' running projects, no reports yet.
        Project? janWoensdag = null;
        for (var d = maandag; d < maandag.AddDays(5); d = d.AddDays(1))
            for (var i = 0; i < mensen.Count; i++)
            {
                var p = Kies(i, Kandidaten(i, d));
                if (i == 0 && d == maandag.AddDays(2)) janWoensdag = p;
                db.Planningen.Add(new Planning { MedewerkerId = mensen[i].Id, ProjectId = p.Id, Datum = d, ToegewezenOp = t });
            }
        // One unapproved intervention to show the conflict flow: Jan helps on a second site on Wednesday.
        var hulp = Lopend(maandag.AddDays(2)).First(p => p != janWoensdag);
        db.Planningen.Add(new Planning { MedewerkerId = mensen[0].Id, ProjectId = hulp.Id, Datum = maandag.AddDays(2), ToegewezenOp = t.AddMinutes(1), IsInterventie = true });
        db.Afwezigheden.Add(new Afwezigheid { MedewerkerId = mensen[5].Id, Van = maandag.AddDays(7), Tot = maandag.AddDays(8), Reden = "Verlof" });
        // Past its end date but still planned next week: the forecast keeps its load and flags it.
        db.Planningen.Add(new Planning { MedewerkerId = mensen[4].Id, ProjectId = Werf("Herstelling Stationsplein").Id, Datum = maandag.AddDays(8), ToegewezenOp = t });
        // Assigned but not started yet: counts in the forecast, not shown as a dashboard card.
        var toekomst = Werf("Appartementen Noord");
        db.Planningen.Add(new Planning { MedewerkerId = mensen[0].Id, ProjectId = toekomst.Id, Datum = PlanningRegels.Maandag(toekomst.Startdatum!.Value).AddDays(7), ToegewezenOp = t });

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    static DagrapportRegel Regel(Project p, int minuten, Random rnd) =>
        new() { ProjectId = p.Id, Status = RegelStatus.GEWERKT, Omschrijving = Omschrijvingen[rnd.Next(Omschrijvingen.Length)], Minuten = minuten };
}
