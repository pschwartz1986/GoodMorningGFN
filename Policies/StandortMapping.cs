using GoodMorningGFN.Models;

namespace GoodMorningGFN.Policies;

public static class StandortMapping
{
    /// <summary>
    /// Ordnet eine SSID einem Standort zu. Es gibt nur zwei bekannte, relevante Netzwerke
    /// (<paramref name="standortSsid"/> und <paramref name="homeofficeSsid"/>) - jede andere SSID
    /// (auch unbekannte/neue Netzwerke) liefert bewusst <see cref="Standort.Unbekannt"/> statt
    /// automatisch "Homeoffice" zu raten. Exakter, case-insensitiver Vergleich (kein "StartsWith"),
    /// damit z. B. "GFN-Gast" nicht versehentlich als "GFN-Schulung" durchgeht.
    /// </summary>
    public static Standort VonSsid(string? ssid, string standortSsid, string homeofficeSsid)
    {
        if (string.IsNullOrWhiteSpace(ssid))
        {
            return Standort.Unbekannt;
        }

        if (ssid.Equals(standortSsid, StringComparison.OrdinalIgnoreCase))
        {
            return Standort.Standort;
        }

        if (ssid.Equals(homeofficeSsid, StringComparison.OrdinalIgnoreCase))
        {
            return Standort.Homeoffice;
        }

        return Standort.Unbekannt;
    }

    public static string ZuLegacyString(Standort standort) => standort switch
    {
        Standort.Standort => "Standort",
        Standort.Homeoffice => "Homeoffice",
        _ => string.Empty
    };
}
