namespace GoodMorningGFN.Policies;

public static class ZeitfensterPolicy
{
    public static bool IstStartenErlaubt(TimeSpan uhrzeit, TimeSpan anmeldenVon, TimeSpan anmeldenBis)
        => uhrzeit >= anmeldenVon && uhrzeit <= anmeldenBis;

    public static bool IstBeendenErlaubt(TimeSpan uhrzeit, TimeSpan beendenAb)
        => uhrzeit >= beendenAb;
}
