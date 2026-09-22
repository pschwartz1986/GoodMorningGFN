using GoodMorningGFN.Models;
using GoodMorningGFN.Policies;

namespace GoodMorningGFN.Tests;

public class StandortMappingTests
{
    private const string StandortSsid = "GFN-Schulung";
    private const string HomeofficeSsid = "KBK";

    [Theory]
    [InlineData("GFN-Schulung", Standort.Standort)]
    [InlineData("gfn-schulung", Standort.Standort)] // case-insensitive
    [InlineData("KBK", Standort.Homeoffice)]
    [InlineData("kbk", Standort.Homeoffice)] // case-insensitive
    public void VonSsid_ordnet_die_beiden_bekannten_Netzwerke_korrekt_zu(string ssid, Standort erwartet)
    {
        Assert.Equal(erwartet, StandortMapping.VonSsid(ssid, StandortSsid, HomeofficeSsid));
    }

    [Theory]
    [InlineData("ZTE Blade A35e")] // Mobile-Hotspot aus der Router-Übersicht
    [InlineData("GFN-Gast")] // ähnlich, aber NICHT das bekannte Schulungs-WLAN - kein Fuzzy-Match mehr
    [InlineData("GFN")]
    [InlineData("KBK-Gast")]
    [InlineData("FRITZ!Box-1234")]
    public void VonSsid_liefert_Unbekannt_fuer_alle_anderen_Netzwerke_statt_automatisch_Homeoffice_zu_raten(string ssid)
    {
        // Genau das war der ursprüngliche Bug: jede nicht erkannte SSID wurde automatisch als
        // "Homeoffice" behandelt. Jetzt bleibt eine unbekannte SSID bewusst unentschieden.
        Assert.Equal(Standort.Unbekannt, StandortMapping.VonSsid(ssid, StandortSsid, HomeofficeSsid));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VonSsid_liefert_Unbekannt_bei_fehlender_SSID(string? ssid)
    {
        Assert.Equal(Standort.Unbekannt, StandortMapping.VonSsid(ssid, StandortSsid, HomeofficeSsid));
    }

    [Theory]
    [InlineData(Standort.Standort, "Standort")]
    [InlineData(Standort.Homeoffice, "Homeoffice")]
    [InlineData(Standort.Unbekannt, "")]
    public void ZuLegacyString_liefert_bisheriges_Stringformat(Standort standort, string erwartet)
    {
        Assert.Equal(erwartet, StandortMapping.ZuLegacyString(standort));
    }
}
