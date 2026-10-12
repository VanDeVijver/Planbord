using Planbord.Core;

namespace Planbord.Tests;

public class RegelsTests
{
    [Fact]
    public void EersteToewijzingIsNormaal_RestIsInterventie()
    {
        var t = DateTime.UtcNow;
        var rijen = new List<Planning>
        {
            new() { Id = 2, ToegewezenOp = t.AddMinutes(5), Goedgekeurd = true },
            new() { Id = 1, ToegewezenOp = t, Goedgekeurd = true },
        };
        PlanningRegels.Herbereken(rijen);
        Assert.False(rijen[1].IsInterventie);
        Assert.True(rijen[0].IsInterventie);
        Assert.True(rijen[0].Goedgekeurd); // approval of an intervention is kept
    }

    static RapportInvoer Dag(params RegelInvoer[] regels) => new()
    {
        Start = new TimeOnly(7, 0), Einde = new TimeOnly(15, 30), PauzeMinuten = 30, Regels = [.. regels],
    };

    [Fact]
    public void EenWerf_KrijgtAutomatischDeNettoTijd()
    {
        var r = Dag(new RegelInvoer { ProjectNaam = "A", Status = RegelStatus.GEWERKT, Omschrijving = "x" },
                    new RegelInvoer { ProjectNaam = "B", Status = RegelStatus.NIET_GEWEEST, Omschrijving = "file", Minuten = 60 });
        Assert.Empty(RapportRegels.Valideer(r, indienen: true));
        Assert.Equal(480, r.Regels[0].Minuten);
        Assert.Equal(0, r.Regels[1].Minuten);
    }

    [Fact]
    public void MeerdereWerven_MoetenExactOptellen()
    {
        var r = Dag(new RegelInvoer { ProjectNaam = "A", Status = RegelStatus.GEWERKT, Omschrijving = "x", Minuten = 300 },
                    new RegelInvoer { ProjectNaam = "B", Status = RegelStatus.GEWERKT, Omschrijving = "y", Minuten = 170 });
        Assert.Single(RapportRegels.Valideer(r, true));
        r.Regels[1].Minuten = 180;
        Assert.Empty(RapportRegels.Valideer(r, true));
    }

    [Fact]
    public void RedenVerplichtBijUitzondering()
    {
        var r = Dag(new RegelInvoer { ProjectNaam = "A", Status = RegelStatus.GEANNULEERD });
        Assert.Single(RapportRegels.Valideer(r, true));
        Assert.Empty(RapportRegels.Valideer(r, false)); // drafts are lenient
    }

    [Theory]
    [InlineData("2026-01-01", "2026-02-01", 1)] // exactly 1 month
    [InlineData("2026-01-01", "2026-02-02", 2)]
    [InlineData("2026-01-01", "2026-03-01", 2)] // 2 months is "<= 3", not "<= 6"
    [InlineData("2026-01-01", "2026-07-01", 3)]
    [InlineData("2026-01-01", "2027-01-01", 4)]
    [InlineData("2026-01-01", "2027-01-02", 5)]
    [InlineData("2026-01-01", null, 5)]          // no end date
    public void LengteCategorie(string start, string? einde, int verwacht) =>
        Assert.Equal(verwacht, ProjectRegels.Categorie(DateOnly.Parse(start), einde is null ? null : DateOnly.Parse(einde)));

    [Theory]
    [InlineData("2026-01-01", null, ProjectStatus.Actief)]
    [InlineData("2026-01-01", "2026-10-12", ProjectStatus.Actief)]
    [InlineData("2026-01-01", "2026-07-12", ProjectStatus.RecentAfgerond)] // exactly 3 months ago
    [InlineData("2026-01-01", "2026-07-11", null)]                         // longer ago: hidden
    [InlineData("2026-11-01", "2027-01-01", null)]                         // not started yet: hidden
    public void ProjectStatusOpDatum(string start, string? einde, ProjectStatus? verwacht) =>
        Assert.Equal(verwacht, ProjectRegels.Status(DateOnly.Parse(start), einde is null ? null : DateOnly.Parse(einde), new DateOnly(2026, 10, 12)));

    [Theory]
    [InlineData("7:30", 450)]
    [InlineData("7,5", 450)]
    [InlineData("8", 480)]
    [InlineData("7:75", null)]
    [InlineData("abc", null)]
    public void DuurParse(string tekst, int? verwacht) => Assert.Equal(verwacht, Duur.Parse(tekst));
}
