using Planbord.Core;

namespace Planbord.Tests;

public class PrognoseTests
{
    static readonly DateOnly Feb = new(2027, 2, 1); // February 2027: 20 working days

    [Fact]
    public void VoorbeeldUitDeNotities_ZesMaandenEnJaarPlusLaten70ProcentVrij()
    {
        var m = Prognose.PerMaand(Feb, 1, [new(Feb, null, 20), new(null, null, 10)], [])[0];
        Assert.Equal(152, m.Capaciteit, 6);   // 20 x 7.6
        Assert.Equal(45.6, m.Last, 6);        // 30% of the capacity
        Assert.Equal(0.7, m.VrijAandeel, 6);  // 26.6 h of every 38 h week free
    }

    [Fact]
    public void VerlofTeltNietMee_EnProjectHalverwegeTeltGedeeltelijk()
    {
        // Absent the first week (5 days); project starts on the 15th (10 working days left).
        var m = Prognose.PerMaand(Feb, 1, [new(new DateOnly(2027, 2, 15), null, 50)], [(Feb, new DateOnly(2027, 2, 5))])[0];
        Assert.Equal(15 * 7.6, m.Capaciteit, 6);
        Assert.Equal(10 * 7.6 * 0.5, m.Last, 6);
    }

    [Fact]
    public void Overboekt_EnVerlengdProjectHoudtZijnLast()
    {
        var verlopen = new ProjectLast(null, new DateOnly(2027, 1, 10), 60, Verlengd: true);
        var m = Prognose.PerMaand(Feb, 1, [verlopen, new(null, null, 50)], [])[0];
        Assert.True(m.Overboekt);
        Assert.False(Prognose.PerMaand(Feb, 1, [verlopen with { Verlengd = false }], [])[0].Overboekt);
    }

    [Fact]
    public void BeschikbaarVanaf_EersteMaandDieDeHeleLooptijdVrijBlijft()
    {
        List<Maand> maanden = [new(Feb, 100, 50), new(Feb.AddMonths(1), 100, 20), new(Feb.AddMonths(2), 100, 20)];
        Assert.Equal(0, Prognose.BeschikbaarVanaf(maanden, 35, 2));
        Assert.Equal(1, Prognose.BeschikbaarVanaf(maanden, 60, 2));
        Assert.Null(Prognose.BeschikbaarVanaf(maanden, 60, 3)); // would run past the horizon
        Assert.Equal(160, Prognose.VrijeUren(maanden, 1, 2));
    }
}
