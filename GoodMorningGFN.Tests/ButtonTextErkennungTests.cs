using GoodMorningGFN.Models;
using GoodMorningGFN.Policies;

namespace GoodMorningGFN.Tests;

public class ButtonTextErkennungTests
{
    [Theory]
    [InlineData("Starten", "", ZeiterfassungAction.Starten)]
    [InlineData("starten", "", ZeiterfassungAction.Starten)]
    [InlineData("", "Starten", ZeiterfassungAction.Starten)]
    [InlineData("Beenden", "", ZeiterfassungAction.Beenden)]
    [InlineData("beenden", "", ZeiterfassungAction.Beenden)]
    [InlineData("", "Beenden", ZeiterfassungAction.Beenden)]
    [InlineData("Irgendwas anderes", "", ZeiterfassungAction.Unbekannt)]
    [InlineData("", "", ZeiterfassungAction.Unbekannt)]
    public void Erkenne_liefert_erwartete_Aktion(string buttonText, string buttonValue, ZeiterfassungAction erwartet)
    {
        Assert.Equal(erwartet, ButtonTextErkennung.Erkenne(buttonText, buttonValue));
    }

    [Fact]
    public void Erkenne_bevorzugt_Starten_wenn_beides_zutrifft()
    {
        // Repliziert exakt die Prüfreihenfolge aus dem Original-Code: Starten wird zuerst geprüft.
        Assert.Equal(ZeiterfassungAction.Starten, ButtonTextErkennung.Erkenne("Starten und Beenden", ""));
    }
}
