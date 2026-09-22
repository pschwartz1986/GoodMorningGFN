using GoodMorningGFN.Policies;

namespace GoodMorningGFN.Tests;

public class ZeitfensterPolicyTests
{
    private static readonly TimeSpan AnmeldenVon = new(7, 30, 0);
    private static readonly TimeSpan AnmeldenBis = new(9, 0, 0);
    private static readonly TimeSpan BeendenAb = new(16, 30, 0);

    [Theory]
    [InlineData(7, 30, 0, true)]
    [InlineData(7, 29, 59, false)]
    [InlineData(9, 0, 0, true)]
    [InlineData(9, 0, 1, false)]
    [InlineData(8, 0, 0, true)]
    public void IstStartenErlaubt_prueft_inklusive_Grenzen(int h, int m, int s, bool erwartet)
    {
        var uhrzeit = new TimeSpan(h, m, s);
        Assert.Equal(erwartet, ZeitfensterPolicy.IstStartenErlaubt(uhrzeit, AnmeldenVon, AnmeldenBis));
    }

    [Theory]
    [InlineData(16, 30, 0, true)]
    [InlineData(16, 29, 59, false)]
    [InlineData(23, 59, 59, true)]
    public void IstBeendenErlaubt_prueft_inklusive_Untergrenze(int h, int m, int s, bool erwartet)
    {
        var uhrzeit = new TimeSpan(h, m, s);
        Assert.Equal(erwartet, ZeitfensterPolicy.IstBeendenErlaubt(uhrzeit, BeendenAb));
    }
}
