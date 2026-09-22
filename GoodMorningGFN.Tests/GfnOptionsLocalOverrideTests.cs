using System.Text;
using GoodMorningGFN.Configuration;
using Microsoft.Extensions.Configuration;

namespace GoodMorningGFN.Tests;

/// <summary>
/// Prüft die Override-Reihenfolge appsettings.json -> appsettings.Local.json (Program.cs lädt
/// in genau dieser Reihenfolge, sodass Local-Werte die Basis-Werte überschreiben). Nutzt
/// In-Memory-JSON-Streams statt echter Dateien, um keine Testartefakte im Projektordner zu hinterlassen.
/// </summary>
public class GfnOptionsLocalOverrideTests
{
    [Fact]
    public void Local_Override_ueberschreibt_Basis_Wert()
    {
        const string basis = """{ "Gfn": { "ChromeProfilePath": "C:\\ChromeAutomation", "DryRun": false } }""";
        const string local = """{ "Gfn": { "ChromeProfilePath": "D:\\AnderesProfilFuerZweitrechner" } }""";

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(basis)))
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(local)))
            .Build();

        var options = new GfnOptions();
        configuration.GetSection("Gfn").Bind(options);

        Assert.Equal(@"D:\AnderesProfilFuerZweitrechner", options.ChromeProfilePath);
        Assert.False(options.DryRun); // von Local nicht überschrieben, bleibt aus Basis erhalten
    }

    [Fact]
    public void Ohne_Local_Override_bleibt_Basis_Wert_erhalten()
    {
        const string basis = """{ "Gfn": { "ChromeProfilePath": "C:\\ChromeAutomation" } }""";

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(basis)))
            .Build();

        var options = new GfnOptions();
        configuration.GetSection("Gfn").Bind(options);

        Assert.Equal(@"C:\ChromeAutomation", options.ChromeProfilePath);
    }
}
