using GoodMorningGFN.Policies;

namespace GoodMorningGFN.Tests;

public class WlanSsidParserTests
{
    // Realistischer Ausschnitt von "netsh wlan show interfaces" auf Deutsch (Windows 11),
    // bewusst mit einer BSSID-Zeile VOR der SSID-Zeile - genau der Fall, der die
    // unverankerte Original-Regex theoretisch hätte fehlmatchen können.
    private const string NetshOutputMitBssidVorSsid = """

        Schnittstellenname : WLAN
            Beschreibung            : Intel(R) Wi-Fi 6 AX201 160MHz
            GUID                    : 12345678-abcd-1234-abcd-1234567890ab
            Physische Adresse       : aa:bb:cc:dd:ee:ff
            Status                  : verbunden
            SSID                    : GFN-Office-5G
            BSSID                   : 11:22:33:44:55:66
            Netzwerktyp             : Infrastruktur
            Funkmodus               : 802.11ac
            Authentifizierung       : WPA2-Personal
            Verschlüsselung         : CCMP
            Verbindungsmodus        : Profil
            Kanal                   : 36
            Empfangsrate (MBit/s)   : 866
            Übertragungsrate (MBit/s) : 866
            Signal                  : 100%
        """;

    private const string NetshOutputHomeWlan = """
        Schnittstellenname : WLAN
            Status                  : verbunden
            SSID                    : FRITZ!Box-7590-AB
            BSSID                   : 66:55:44:33:22:11
        """;

    private const string NetshOutputOhneVerbindung = """
        Schnittstellenname : WLAN
            Status                  : nicht verbunden
        """;

    [Fact]
    public void ExtractSsid_findet_SSID_Zeile_und_ignoriert_folgende_BSSID_Zeile()
    {
        Assert.Equal("GFN-Office-5G", WlanSsidParser.ExtractSsid(NetshOutputMitBssidVorSsid));
    }

    [Fact]
    public void ExtractSsid_findet_Heim_SSID()
    {
        Assert.Equal("FRITZ!Box-7590-AB", WlanSsidParser.ExtractSsid(NetshOutputHomeWlan));
    }

    [Fact]
    public void ExtractSsid_liefert_null_wenn_keine_SSID_Zeile_vorhanden()
    {
        Assert.Null(WlanSsidParser.ExtractSsid(NetshOutputOhneVerbindung));
    }

    [Fact]
    public void ExtractSsid_matcht_nicht_auf_BSSID_Zeile_wenn_SSID_fehlt()
    {
        const string nurBssid = "    BSSID                   : 11:22:33:44:55:66";
        Assert.Null(WlanSsidParser.ExtractSsid(nurBssid));
    }
}
