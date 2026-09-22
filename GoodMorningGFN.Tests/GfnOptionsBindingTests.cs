using GoodMorningGFN.Configuration;
using Microsoft.Extensions.Configuration;

namespace GoodMorningGFN.Tests;

public class GfnOptionsBindingTests
{
    // Bindet die echte appsettings.json (per Link ins Testprojekt kopiert) - stellt sicher,
    // dass die Config-Externalisierung dieselben Werte liefert wie vorher die Main-Konstanten,
    // ohne dafür einen echten Programmlauf (inkl. Playwright/Chrome) zu brauchen.
    private static GfnOptions LadeOptionenAusEchterAppsettings()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var options = new GfnOptions();
        configuration.GetSection("Gfn").Bind(options);
        return options;
    }

    [Fact]
    public void Appsettings_bindet_dieselben_Default_Werte_wie_vorher_hartkodiert()
    {
        var options = LadeOptionenAusEchterAppsettings();

        Assert.False(options.DryRun);
        Assert.Equal(@"C:\ChromeAutomation", options.ChromeProfilePath);
        Assert.Equal("https://lernplattform.gfn.de/", options.GfnStartUrl);
        Assert.Equal("https://lernplattform.gfn.de/local/anmeldung/anwesenheit.php", options.AnwesenheitUrl);
        Assert.Equal(new TimeSpan(7, 30, 0), options.AnmeldenVon);
        Assert.Equal(new TimeSpan(9, 0, 0), options.AnmeldenBis);
        Assert.Equal(new TimeSpan(16, 30, 0), options.BeendenAb);
        Assert.Equal(TimeSpan.FromSeconds(5), options.PruefIntervall);
        Assert.Equal("GFN-Schulung", options.StandortSsid);
        Assert.Equal("KBK", options.HomeofficeSsid);
    }
}
