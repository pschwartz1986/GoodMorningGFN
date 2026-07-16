# GoodMorningGFN

Automatisierung der GFN-Zeiterfassung über Moodle mit Playwright.

## Voraussetzungen

- Windows 10/11 (x64)
- .NET 10 Runtime (wird bei selbsthaltendem Publish nicht benötigt)
- Chrome-Profil unter `C:\ChromeAutomation` (wird automatisch verwendet)

## Einrichtung

### Variante A: Direkt starten
1. In den Ordner `publish` wechseln
2. `GoodMorningGFN.exe` doppelklicken oder per CMD starten

### Variante B: Mit Autostart
1. `setup.cmd` als Administrator ausführen
2. Im Menü "Installieren" wählen
3. Die Aufgabenplanung richtet den Autostart bei Windows-Anmeldung ein

## Funktionsweise

- **Login:** Automatischer Login über gespeicherte Chrome-Anmeldedaten
- **Arbeitsort:** Automatische Erkennung über WLAN-SSID (`GFN*` = Standort, alles andere = Homeoffice)
- **Starten:** Automatisch zwischen 07:30 und 09:00
- **Beenden:** Automatisch ab 16:30
- **Anwesenheitsprüfung:** Einmalige Prüfung der Eintragung nach erfolgreichem Start
- **Monitoring:** Dauerhafte Überwachung im 5-Sekunden-Intervall

## Konfiguration

Die zentralen Einstellungen befinden sich in `Program.cs`:

| Einstellung | Standard | Beschreibung |
|-------------|----------|--------------|
| `DRY_RUN` | `false` | Bei `true` werden keine Aktionen ausgeführt |
| `anmeldenVon` | 07:30 | Frühester Start der Zeiterfassung |
| `anmeldenBis` | 09:00 | Spätester Start der Zeiterfassung |
| `pruefIntervall` | 5 Sekunden | Intervall der Dauerüberwachung |
| `chromeProfilePath` | `C:\ChromeAutomation` | Pfad zum persistenten Chrome-Profil |

## Wichtige Hinweise

- Der Browser wird vom Skript **nicht** geschlossen
- Die Zeiterfassung kann jederzeit manuell über die Moodle-Oberfläche gesteuert werden
- Bei Standortwechsel wird `arbeitsort.txt` automatisch aktualisiert
- Ab nächster Woche: Automatische Standorterkennung durch GFN

## Entwickler

- `Program.cs` – Hauptlogik
- `setup.cmd` / `setup-autostart.ps1` – Windows-Autostart
- `publish/` – Selbsthaltender Build für Windows x64

## Lizenz

Privatprojekt / interner Einsatz.
