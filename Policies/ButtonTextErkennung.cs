using GoodMorningGFN.Models;

namespace GoodMorningGFN.Policies;

public static class ButtonTextErkennung
{
    public static ZeiterfassungAction Erkenne(string buttonText, string buttonValue)
    {
        bool istStarten = buttonText.Contains("Starten", StringComparison.OrdinalIgnoreCase) ||
                           buttonValue.Contains("Starten", StringComparison.OrdinalIgnoreCase);
        bool istBeenden = buttonText.Contains("Beenden", StringComparison.OrdinalIgnoreCase) ||
                           buttonValue.Contains("Beenden", StringComparison.OrdinalIgnoreCase);

        if (istStarten)
        {
            return ZeiterfassungAction.Starten;
        }

        if (istBeenden)
        {
            return ZeiterfassungAction.Beenden;
        }

        return ZeiterfassungAction.Unbekannt;
    }
}
