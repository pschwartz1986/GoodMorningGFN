namespace GoodMorningGFN.Configuration;

public sealed class GfnOptions
{
    public bool DryRun { get; set; } = false;
    public string ChromeProfilePath { get; set; } = @"C:\ChromeAutomation";
    public string GfnStartUrl { get; set; } = "https://lernplattform.gfn.de/";
    public string AnwesenheitUrl { get; set; } = "https://lernplattform.gfn.de/local/anmeldung/anwesenheit.php";
    public TimeSpan AnmeldenVon { get; set; } = new(7, 30, 0);
    public TimeSpan AnmeldenBis { get; set; } = new(9, 0, 0);
    public TimeSpan BeendenAb { get; set; } = new(16, 30, 0);
    public TimeSpan PruefIntervall { get; set; } = TimeSpan.FromSeconds(5);

    // Es gibt genau zwei bekannte, relevante WLANs (siehe Router-Übersicht "Bekannte Netzwerke").
    // Jede andere SSID (fremdes Netz, Mobile-Hotspot, kein WLAN) ist bewusst NICHT automatisch
    // "Homeoffice" - das war der ursprüngliche Bug. Unbekannte SSIDs bleiben unentschieden.
    public string StandortSsid { get; set; } = "GFN-Schulung";
    public string HomeofficeSsid { get; set; } = "KBK";
}
