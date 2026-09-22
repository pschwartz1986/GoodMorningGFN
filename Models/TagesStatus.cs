namespace GoodMorningGFN.Models;

/// <summary>
/// Zustand mit Tages-Lifetime. <see cref="ResetFuerNeuenTag"/> ist die EINZIGE Stelle, an der
/// diese Felder zurückgesetzt werden dürfen — genau das hat beim ursprünglichen Homeoffice-Bug
/// gefehlt (AktuellerStandort blieb über Tage hinweg hängen, weil kein zentraler Reset existierte).
/// </summary>
public sealed class TagesStatus
{
    public bool ZeiterfassungGetriggert { get; set; }
    public bool AnwesenheitGeprueft { get; set; }
    public string? AnwesenheitStatus { get; set; }
    public string? AktuellerStandort { get; set; }
    public DateTime LetztePruefungDatum { get; private set; } = DateTime.MinValue;

    public void ResetFuerNeuenTag(DateTime heute)
    {
        ZeiterfassungGetriggert = false;
        AnwesenheitGeprueft = false;
        AnwesenheitStatus = null;
        AktuellerStandort = null;
        LetztePruefungDatum = heute;
    }
}
