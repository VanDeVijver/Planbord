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

    /// Destructive: wipes all planning data (not user accounts) and seeds projects with crews,
    /// holidays, a planned current week and closed reports for every workday since 1 January 2025.
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

        // 0 Jan, 1 Sofie, 2 Tom, 3 Lotte, 4 Kevin, 5 Ines
        var mensen = new[] { "Jan Peeters", "Sofie Janssens", "Tom Maes", "Lotte Claes", "Kevin Wouters", "Ines Goossens" }
            .Select(n => new Medewerker { Naam = n }).ToList();
        int[] iedereen = [0, 1, 2, 3, 4, 5];

        var v = vandaag;
        var ploegen = new Dictionary<Project, int[]>();
        Project P(string naam, string adres, DateOnly? start, DateOnly? einde, int[] ploeg)
        {
            var p = new Project { Naam = naam, Adres = adres, Startdatum = start, Einddatum = einde };
            ploegen[p] = ploeg;
            return p;
        }

        // Crews are chosen so every employee has at least two active or recently finished projects per
        // length category, while the planned load differs: Ines has the most room, Lotte is overbooked.
        var terBeke = P("Onderhoud School Ter Beke", "Bekestraat 30, Duffel", v.AddDays(-10), v.AddDays(12), [0, 3]);
        var kerkstraat = P("Renovatie Kerkstraat", "Kerkstraat 12, Mechelen", v.AddMonths(-2), v.AddMonths(1), [0, 1]);
        kerkstraat.Taken = [new() { Omschrijving = "Stelling afbreken" }, new() { Omschrijving = "Kabelgoten plaatsen" }];
        var linde = P("Nieuwbouw De Linde", "Lindelaan 4, Lier", v.AddMonths(-4), v.AddMonths(10), [3, 4, 5]);
        linde.Taken = [new() { Omschrijving = "Verdeelbord aansluiten" }];
        var stationsplein = P("Herstelling Stationsplein", "Stationsplein 1, Mechelen", v.AddDays(-25), v.AddDays(-5), [1, 2, 4, 5]);
        var werven = new List<Project>
        {
            // <= 1 month
            terBeke,
            P("Spoedklus Sint-Romboutscollege", "Sint-Romboutskerkhof 4, Mechelen", v.AddDays(-5), v.AddDays(18), [3]),
            stationsplein,
            P("Keuring Sporthal De Nete", "Netelaan 9, Lier", v.AddDays(-40), v.AddDays(-20), [0, 1, 2, 5]),
            P("Noodherstelling Hangar 3", "Luchthavenlaan 3, Duffel", v.AddDays(-60), v.AddDays(-45), [0, 3, 4]),
            // <= 3 months
            kerkstraat,
            P("Verlichting Parking Oost", "Oostweg 3, Mechelen", v.AddDays(-50), v.AddDays(20), [2, 4]),
            P("Laadpalen Gemeentehuis", "Grote Markt 1, Duffel", v.AddDays(-80), v.AddDays(-15), iedereen),
            P("Bekabeling Kinderdagverblijf", "Speelpleinstraat 8, Lier", v.AddDays(-100), v.AddDays(-30), [3, 5]),
            // <= 6 months
            P("Sanitair Rusthuis Ter Linden", "Lindestraat 20, Lier", v.AddMonths(-2), v.AddMonths(2), [1, 3]),
            P("Brandmelding Bibliotheek", "Bibliotheekplein 2, Mechelen", v.AddMonths(-4), v.AddMonths(1), [2, 5]),
            P("Dakwerken Vrijthof", "Vrijthof 5, Mechelen", v.AddMonths(-5), v.AddDays(-21), iedereen),
            P("Renovatie Turnzaal", "Sportlaan 11, Duffel", v.AddDays(-150), v.AddDays(-40), [0, 4]),
            // <= 12 months
            P("Loods Haven", "Havenweg 2, Antwerpen", v.AddMonths(-3), v.AddMonths(6), [0, 3, 4]),
            P("Zonnepanelen Industriepark", "Industriepark 14, Duffel", v.AddMonths(-8), v.AddMonths(2), [1, 2, 5]),
            P("Schoolcampus West", "Westlaan 40, Mechelen", v.AddMonths(-11), v.AddMonths(-1), iedereen),
            // 1 year+
            linde,
            P("Ziekenhuis Campus Noord", "Noordring 100, Antwerpen", v.AddMonths(-15), v.AddMonths(6), [0, 1, 2, 3]),
            P("Uitbreiding Fabriek Noord", "Fabriekstraat 3, Antwerpen", v.AddMonths(-16), v.AddMonths(-2), [1, 2, 4, 5]),
            // Finished long ago: only in the hours (last year / all time)
            P("Kantoor Zuidpark", "Zuidpark 1, Gent", v.AddMonths(-9), v.AddMonths(-5), iedereen),
            P("Winkelcentrum Ring", "Ringlaan 50, Mechelen", v.AddMonths(-21), v.AddMonths(-10), iedereen),
            P("Renovatie Stadhuis", "Stadhuisplein 1, Lier", v.AddMonths(-22), v.AddMonths(-14), iedereen),
            P("Verbouwing Bakkerij Smets", "Bruul 18, Mechelen", v.AddMonths(-14), v.AddMonths(-12), iedereen),
            P("Herstelling Brouwerij", "Brouwerijstraat 6, Lier", v.AddMonths(-16), v.AddMonths(-16).AddDays(20), iedereen),
            P("Keuken Restaurant Ter Dijk", "Dijkstraat 2, Mechelen", v.AddMonths(-19), v.AddMonths(-14), iedereen),
        };
        var werkplaats = new Project { Naam = "Werkplaats", Adres = "Industrieweg 8, Mechelen", Startdatum = v.AddYears(-2) };
        var toekomst = new Project { Naam = "Appartementen Noord", Adres = "Noorderlaan 77, Antwerpen", Startdatum = v.AddMonths(1), Einddatum = v.AddMonths(13) };
        db.AddRange(mensen);
        db.AddRange(werven);
        db.AddRange(werkplaats, toekomst);
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
        // Days worked per (employee, project): each day goes to the least-used running project of the
        // employee's crews, so short jobs get picked too and every crew member is really assigned.
        var gebruikt = new Dictionary<(int, Project), int>();
        List<Project> Kandidaten(int i, DateOnly d)
        {
            var lopend = werven.Where(p => p.Startdatum <= d && (p.Einddatum is null || p.Einddatum >= d)).ToList();
            var eigen = lopend.Where(p => ploegen[p].Contains(i)).ToList();
            return eigen.Count > 0 ? eigen : lopend.Count > 0 ? lopend : [werkplaats];
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
        for (var d = maandag; d < maandag.AddDays(5); d = d.AddDays(1))
            for (var i = 0; i < mensen.Count; i++)
                db.Planningen.Add(new Planning { MedewerkerId = mensen[i].Id, ProjectId = Kies(i, Kandidaten(i, d)).Id, Datum = d, ToegewezenOp = t });
        // One unapproved intervention to show the conflict flow.
        db.Planningen.Add(new Planning { MedewerkerId = mensen[0].Id, ProjectId = werkplaats.Id, Datum = maandag.AddDays(2), ToegewezenOp = t.AddMinutes(1), IsInterventie = true });
        db.Afwezigheden.Add(new Afwezigheid { MedewerkerId = mensen[5].Id, Van = maandag.AddDays(7), Tot = maandag.AddDays(8), Reden = "Verlof" });
        // Past its end date but still planned next week: the forecast keeps its load and flags it.
        db.Planningen.Add(new Planning { MedewerkerId = mensen[4].Id, ProjectId = stationsplein.Id, Datum = maandag.AddDays(8), ToegewezenOp = t });
        // Assigned but not started yet: counts in the forecast, not shown as a dashboard card.
        db.Planningen.Add(new Planning { MedewerkerId = mensen[0].Id, ProjectId = toekomst.Id, Datum = PlanningRegels.Maandag(toekomst.Startdatum!.Value).AddDays(7), ToegewezenOp = t });

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    static DagrapportRegel Regel(Project p, int minuten, Random rnd) =>
        new() { ProjectId = p.Id, Status = RegelStatus.GEWERKT, Omschrijving = Omschrijvingen[rnd.Next(Omschrijvingen.Length)], Minuten = minuten };
}
