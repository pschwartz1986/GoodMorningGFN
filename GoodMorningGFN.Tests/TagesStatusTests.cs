using GoodMorningGFN.Models;

namespace GoodMorningGFN.Tests;

public class TagesStatusTests
{
    [Fact]
    public void ResetFuerNeuenTag_setzt_alle_Tagesfelder_zurueck()
    {
        var status = new TagesStatus
        {
            ZeiterfassungGetriggert = true,
            AnwesenheitGeprueft = true,
            AnwesenheitStatus = "irgendein Status",
            AktuellerStandort = "Homeoffice"
        };

        var heute = new DateTime(2026, 9, 23);
        status.ResetFuerNeuenTag(heute);

        Assert.False(status.ZeiterfassungGetriggert);
        Assert.False(status.AnwesenheitGeprueft);
        Assert.Null(status.AnwesenheitStatus);
        Assert.Null(status.AktuellerStandort);
        Assert.Equal(heute, status.LetztePruefungDatum);
    }

    [Fact]
    public void ResetFuerNeuenTag_ist_mehrfach_aufrufbar()
    {
        var status = new TagesStatus();
        status.ResetFuerNeuenTag(new DateTime(2026, 9, 22));
        status.AktuellerStandort = "Standort";
        status.ResetFuerNeuenTag(new DateTime(2026, 9, 23));

        Assert.Null(status.AktuellerStandort);
        Assert.Equal(new DateTime(2026, 9, 23), status.LetztePruefungDatum);
    }

    [Fact]
    public void SessionStatus_wird_nicht_von_TagesStatus_beeinflusst()
    {
        // SessionStatus (FirstRun/LastLoginState) ist bewusst eine eigene Klasse ohne
        // gemeinsamen Reset mit TagesStatus - genau das verhindert den ursprünglichen Bug-Typ,
        // bei dem ein Tageswechsel-Reset versehentlich Prozess-Lifetime-Felder mitgerissen hätte.
        var session = new SessionStatus { FirstRun = false, LastLoginState = true };
        var tagesStatus = new TagesStatus();
        tagesStatus.ResetFuerNeuenTag(DateTime.Today);

        Assert.False(session.FirstRun);
        Assert.True(session.LastLoginState);
    }
}
