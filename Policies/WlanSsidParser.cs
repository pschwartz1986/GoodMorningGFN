using System.Text.RegularExpressions;

namespace GoodMorningGFN.Policies;

public static class WlanSsidParser
{
    // Zeilenverankert (^...$ mit Multiline), damit z. B. eine vorangehende "BSSID"-Zeile
    // in der netsh-Ausgabe nicht versehentlich mitgematcht wird.
    private static readonly Regex SsidLinePattern = new(
        @"^\s*SSID\s*:\s*(.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public static string? ExtractSsid(string netshOutput)
    {
        var match = SsidLinePattern.Match(netshOutput);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
