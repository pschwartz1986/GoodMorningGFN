using Microsoft.Playwright;
using static Microsoft.Playwright.Playwright;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace GoodMorningGFN;

static class Program
{
    private const string GfnStartUrl = "https://lernplattform.gfn.de/";

    [STAThread]
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        bool DRY_RUN = false;
        string chromeProfilePath = @"C:\ChromeAutomation";
        string gfnStartUrl = GfnStartUrl;
        string anwesenheitUrl = "https://lernplattform.gfn.de/local/anmeldung/anwesenheit.php";

        TimeSpan anmeldenVon = new TimeSpan(7, 30, 0);
        TimeSpan anmeldenBis = new TimeSpan(9, 0, 0);
        TimeSpan pruefIntervall = TimeSpan.FromSeconds(5);

        string arbeitsortPfad = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "arbeitsort.txt");
        string? aktuellerStandort = null;

        if (File.Exists(arbeitsortPfad))
        {
            try { aktuellerStandort = File.ReadAllText(arbeitsortPfad).Trim(); }
            catch { }
        }

        Console.WriteLine("========================================");
        Console.WriteLine("            GoodMorningGFN");
        Console.WriteLine("========================================");
        Console.WriteLine();

        Console.WriteLine($"[CONFIG] DRY_RUN: {DRY_RUN}");
        Console.WriteLine($"[CONFIG] Anmeldung/Starten erlaubt: {anmeldenVon:hh\\:mm} - {anmeldenBis:hh\\:mm}");
        Console.WriteLine($"[CONFIG] Beenden ab: 16:30");
        Console.WriteLine($"[CONFIG] Prüfintervall: {pruefIntervall.TotalSeconds} Sekunden");
        Console.WriteLine($"[CONFIG] Chrome-Profil: {chromeProfilePath}");
        Console.WriteLine();

        try
        {
            Console.WriteLine("[1] Starte Playwright ...");
            var playwright = await Playwright.CreateAsync();
            Console.WriteLine("[OK] Playwright gestartet.");

            Console.WriteLine("[2] Starte Chrome mit persistentem Profil ...");
            var context = await playwright.Chromium.LaunchPersistentContextAsync(
                chromeProfilePath,
                new BrowserTypeLaunchPersistentContextOptions
                {
                    Channel = "chrome",
                    Headless = false
                });
            Console.WriteLine("[OK] Chrome gestartet.");

            IPage page = await GetOrCreatePageAsync(context);
            AttachPageHandlers(page);

            bool firstRun = true;
            bool? lastLoginState = null;
            bool zeiterfassungGetriggert = false;
            DateTime letztePruefungDatum = DateTime.MinValue;
            bool anwesenheitGeprueft = false;
            string? anwesenheitStatus = null;

            Console.WriteLine("[3] Öffne GFN ...");
            await SafeGotoAsync(page, gfnStartUrl);

            lastLoginState = IsLoginPage(page);

            if (lastLoginState.Value)
            {
                await page.ScreenshotAsync(new() { Path = "01_loginseite.png", FullPage = true });
                Console.WriteLine("[OK] Screenshot gespeichert: 01_loginseite.png");
            }
            else
            {
                await page.ScreenshotAsync(new() { Path = "03_startseite.png", FullPage = true });
                Console.WriteLine("[OK] Screenshot gespeichert: 03_startseite.png");
            }

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine(" DAUERÜBERWACHUNG AKTIV");
            Console.WriteLine("========================================");
            Console.WriteLine("Q im Terminal = Programm beenden");
            Console.WriteLine("Browser wird vom Skript NICHT geschlossen");
            Console.WriteLine("Login wird immer erlaubt, damit die Prüfung möglich ist");
            Console.WriteLine("Starten nur zwischen 07:30 und 09:00");
            Console.WriteLine("Beenden ab 16:30");
            Console.WriteLine("========================================");
            Console.WriteLine();

            while (true)
            {
                bool handleKey = !Console.IsOutputRedirected && !Console.IsInputRedirected && !Console.IsErrorRedirected;

                if (handleKey && Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        Console.WriteLine();
                        Console.WriteLine("[INFO] Q erkannt. Programm wird beendet.");
                        Console.WriteLine("[INFO] Browser wird nicht aktiv geschlossen.");
                        break;
                    }
                }

                DateTime jetzt = DateTime.Now;
                TimeSpan uhrzeit = jetzt.TimeOfDay;
                DateTime heute = jetzt.Date;

                if (letztePruefungDatum != heute)
                {
                    Console.WriteLine($"[NEUER TAG] Reset Tagesstatus für {heute:yyyy-MM-dd}");
                    zeiterfassungGetriggert = false;
                    anwesenheitGeprueft = false;
                    anwesenheitStatus = null;
                    letztePruefungDatum = heute;
                }

                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine($"[CHECK] {jetzt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine("========================================");

                if (!string.IsNullOrEmpty(anwesenheitStatus))
                {
                    Console.WriteLine(anwesenheitStatus);
                }

                try
                {
                    page = await EnsurePageAsync(context, page);

                    if (page.IsClosed)
                    {
                        await WaitNextAsync(pruefIntervall);
                        continue;
                    }

                    bool currentLoginState = IsLoginPage(page);

                    if (currentLoginState)
                    {
                        if (firstRun || lastLoginState != true)
                        {
                            Console.WriteLine("[STATUS] Loginseite erkannt.");
                            await page.ScreenshotAsync(new() { Path = "01_loginseite.png", FullPage = true });
                            Console.WriteLine("[OK] Screenshot gespeichert: 01_loginseite.png");
                        }

                        Console.WriteLine("[INFO] Login ist grundsätzlich erlaubt, damit die Zeiterfassung geprüft werden kann.");
                        Console.WriteLine("[INFO] Warte kurz auf Chrome-Autofill ...");
                        await page.WaitForTimeoutAsync(2500);

                        bool loginButtonGefunden = await TryClickLoginAsync(page, DRY_RUN);

                        if (!loginButtonGefunden)
                        {
                            Console.WriteLine("[WARN] Kein Login-Button gefunden.");
                            Console.WriteLine("[INFO] Prüfe später erneut.");
                            await WaitNextAsync(pruefIntervall);
                            continue;
                        }

                        if (DRY_RUN)
                        {
                            Console.WriteLine("[DRY_RUN] Login wäre jetzt geklickt worden.");
                            Console.WriteLine("[INFO] Weil DRY_RUN aktiv ist, bleibst du auf der Loginseite.");
                            Console.WriteLine("[HINWEIS] Für echten Login: DRY_RUN = false setzen.");
                        }
                        else
                        {
                            Console.WriteLine("[OK] Login wurde ausgelöst.");
                            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
                            await page.WaitForTimeoutAsync(1500);

                            Console.WriteLine($"[URL NACH LOGIN]   {page.Url}");
                            Console.WriteLine($"[TITLE NACH LOGIN] {await page.TitleAsync()}");

                            await page.ScreenshotAsync(new() { Path = "02_nach_login.png", FullPage = true });
                            Console.WriteLine("[OK] Screenshot gespeichert: 02_nach_login.png");

                            if (IsLoginPage(page))
                            {
                                Console.WriteLine("[WARN] Login scheint fehlgeschlagen. Noch auf Loginseite.");
                            }
                            else
                            {
                                Console.WriteLine("[VERIFIZIERT] Login erfolgreich.");
                            }
                        }

                        lastLoginState = true;
                        firstRun = false;
                        await WaitNextAsync(pruefIntervall);
                        continue;
                    }

                    Console.WriteLine("[STATUS] Bereits auf Startseite.");

                    if (firstRun)
                    {
                        await page.ScreenshotAsync(new() { Path = "03_startseite.png", FullPage = true });
                        Console.WriteLine("[OK] Screenshot gespeichert: 03_startseite.png");
                    }

                    lastLoginState = false;
                    firstRun = false;

                    await TryOpenBlockDrawerAsync(page);

                    var form = page.Locator("section[data-block='loginstat'] form[action*='starten=1']");
                    int formCount = await form.CountAsync();

                    if (formCount > 0)
                    {
                        if (!zeiterfassungGetriggert && string.IsNullOrEmpty(aktuellerStandort))
                        {
                            Console.WriteLine("[STATUS] Ermittle Arbeitsort aus WLAN ...");
                            aktuellerStandort = ErmittleArbeitsortAusWlan();
                            Console.WriteLine($"[INFO] Auswahl: {aktuellerStandort}");

                            try { File.WriteAllText(arbeitsortPfad, aktuellerStandort ?? string.Empty); }
                            catch { }
                        }

                        if (!zeiterfassungGetriggert && !string.IsNullOrEmpty(aktuellerStandort))
                        {
                            await TrySetArbeitsortAsync(page, aktuellerStandort);
                        }

                        await page.WaitForTimeoutAsync(500);

                        bool startFenster = uhrzeit >= anmeldenVon && uhrzeit <= anmeldenBis;
                        if (!startFenster)
                        {
                            Console.WriteLine($"[BLOCKIERT] Starten ist nur zwischen {anmeldenVon:hh\\:mm} und {anmeldenBis:hh\\:mm} erlaubt.");
                            Console.WriteLine("[ERGEBNIS] Keine Aktion durchgeführt.");
                            await WaitNextAsync(pruefIntervall);
                            continue;
                        }

                        if (DRY_RUN)
                        {
                            Console.WriteLine("[DRY_RUN] Starten wäre jetzt geklickt worden.");
                        }
                        else
                        {
                            var submit = form.Locator("input[type='submit'][value='Starten']");
                            if (await submit.CountAsync() > 0)
                            {
                                await submit.Nth(0).ClickAsync();
                                Console.WriteLine("[OK] Starten wurde geklickt.");

                                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
                                await page.WaitForTimeoutAsync(1500);

                                await page.ScreenshotAsync(new() { Path = "04_nach_starten.png", FullPage = true });
                                Console.WriteLine("[OK] Screenshot gespeichert: 04_nach_starten.png");

                                var beendenButton = page.Locator("section[data-block='loginstat'] button");
                                bool beendenGefunden = await beendenButton.CountAsync() > 0 &&
                                    (await beendenButton.Nth(0).InnerTextAsync()).Contains("Beenden", StringComparison.OrdinalIgnoreCase);

                                if (beendenGefunden)
                                {
                                    Console.WriteLine("[VERIFIZIERT] Zeiterfassung wurde gestartet.");
                                    zeiterfassungGetriggert = true;

                                    if (!DRY_RUN && !anwesenheitGeprueft)
                                    {
                                        Console.WriteLine("[INFO] Prüfe Anwesenheitseintrag nach Start ...");
                                        anwesenheitStatus = await TryGetTodaysAttendanceStatusAsync(page, anwesenheitUrl);
                                        anwesenheitGeprueft = true;
                                        
                                        if (!string.IsNullOrEmpty(anwesenheitStatus))
                                        {
                                            Console.WriteLine(anwesenheitStatus);
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("[WARN] Zeiterfassung scheint nicht gestartet zu sein. Beenden-Button nicht gefunden.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("[WARN] Starten-Submit nicht gefunden.");
                            }
                        }

                        await WaitNextAsync(pruefIntervall);
                        continue;
                    }

                    var zeiterfassungButtons = page.Locator("section[data-block='loginstat'] button, section[data-block='loginstat'] input[type='submit']");
                    int buttonCount = await zeiterfassungButtons.CountAsync();

                    if (buttonCount == 0)
                    {
                        if (IsLoginPage(page))
                        {
                            Console.WriteLine("[INFO] Keine Zeiterfassungs-Buttons gefunden. Grund: Benutzer ist abgemeldet.");
                        }
                        else
                        {
                            var blockText = await page.Locator("section[data-block='loginstat']").Nth(0).InnerTextAsync();
                            bool hatStartzeit = blockText.Contains("Startzeit", StringComparison.OrdinalIgnoreCase);
                            bool hatEndzeit = blockText.Contains("Endzeit", StringComparison.OrdinalIgnoreCase);

                            if (hatStartzeit && hatEndzeit)
                            {
                                Console.WriteLine("[INFO] Zeiterfassung ist beendet. Block zeigt nur Start-/Endzeit an.");
                                zeiterfassungGetriggert = false;
                            }
                            else
                            {
                                Console.WriteLine("[WARN] Kein Starten/Beenden-Button gefunden.");
                            }
                        }
                        await WaitNextAsync(pruefIntervall);
                        continue;
                    }

                    var button = zeiterfassungButtons.Nth(0);
                    string buttonText = (await button.InnerTextAsync()).Trim();
                    string buttonValue = await button.GetAttributeAsync("value") ?? string.Empty;

                    bool istStarten = buttonText.Contains("Starten", StringComparison.OrdinalIgnoreCase) ||
                                      buttonValue.Contains("Starten", StringComparison.OrdinalIgnoreCase);
                    bool istBeenden = buttonText.Contains("Beenden", StringComparison.OrdinalIgnoreCase) ||
                                      buttonValue.Contains("Beenden", StringComparison.OrdinalIgnoreCase);

                    if (istStarten)
                    {
                        Console.WriteLine("[STATUS] Zeiterfassung ist offenbar NICHT gestartet.");
                        Console.WriteLine("[AKTION] Mögliche Aktion: STARTEN");

                        bool startFenster = uhrzeit >= anmeldenVon && uhrzeit <= anmeldenBis;

                        if (!startFenster)
                        {
                            Console.WriteLine($"[BLOCKIERT] Starten ist nur zwischen {anmeldenVon:hh\\:mm} und {anmeldenBis:hh\\:mm} erlaubt.");
                        }
                        else
                        {
                            if (DRY_RUN)
                            {
                                Console.WriteLine("[DRY_RUN] Starten wäre jetzt geklickt worden.");
                            }
                            else
                            {
                                await button.ClickAsync();
                                Console.WriteLine("[OK] Starten wurde geklickt.");

                                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
                                await page.WaitForTimeoutAsync(1500);

                                await page.ScreenshotAsync(new() { Path = "04_nach_starten.png", FullPage = true });
                                Console.WriteLine("[OK] Screenshot gespeichert: 04_nach_starten.png");

                                var nachButton = page.Locator("section[data-block='loginstat'] button, section[data-block='loginstat'] input[type='submit']");
                                int nachButtonCount = await nachButton.CountAsync();
                                bool istJetztBeenden = false;

                                if (nachButtonCount > 0)
                                {
                                    string nachButtonText = (await nachButton.Nth(0).InnerTextAsync()).Trim();
                                    istJetztBeenden = nachButtonText.Contains("Beenden", StringComparison.OrdinalIgnoreCase);
                                }

                                if (istJetztBeenden)
                                {
                                    Console.WriteLine("[VERIFIZIERT] Zeiterfassung wurde gestartet.");
                                    zeiterfassungGetriggert = true;

                                    if (!DRY_RUN && !anwesenheitGeprueft)
                                    {
                                        Console.WriteLine("[INFO] Prüfe Anwesenheitseintrag nach Start ...");
                                        anwesenheitStatus = await TryGetTodaysAttendanceStatusAsync(page, anwesenheitUrl);
                                        anwesenheitGeprueft = true;
                                        
                                        if (!string.IsNullOrEmpty(anwesenheitStatus))
                                        {
                                            Console.WriteLine(anwesenheitStatus);
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("[WARN] Zeiterfassung scheint nicht gestartet zu sein. Beenden-Button nicht gefunden.");
                                }
                            }
                        }
                    }
                    else if (istBeenden)
                    {
                        Console.WriteLine("[STATUS] Zeiterfassung läuft offenbar.");
                        Console.WriteLine("[AKTION] Mögliche Aktion: BEENDEN");

                        if (!anwesenheitGeprueft && !DRY_RUN)
                        {
                            Console.WriteLine("[INFO] Prüfe Anwesenheitseintrag während laufender Zeiterfassung ...");
                            anwesenheitStatus = await TryGetTodaysAttendanceStatusAsync(page, anwesenheitUrl);
                            anwesenheitGeprueft = true;

                            if (!string.IsNullOrEmpty(anwesenheitStatus))
                            {
                                Console.WriteLine(anwesenheitStatus);
                            }
                        }

                        bool endeFenster = uhrzeit >= new TimeSpan(16, 30, 0);

                        if (!endeFenster)
                        {
                            Console.WriteLine("[BLOCKIERT] Beenden ist erst ab 16:30 erlaubt.");
                        }
                        else
                        {
                            if (DRY_RUN)
                            {
                                Console.WriteLine("[DRY_RUN] Beenden wäre jetzt geklickt worden.");
                            }
                            else
                            {
                                await button.ClickAsync();
                                Console.WriteLine("[OK] Beenden wurde geklickt.");

                                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
                                await page.WaitForTimeoutAsync(1500);

                                await page.ScreenshotAsync(new() { Path = "05_nach_beenden.png", FullPage = true });
                                Console.WriteLine("[OK] Screenshot gespeichert: 05_nach_beenden.png");

                                var nachButton = page.Locator("section[data-block='loginstat'] button, section[data-block='loginstat'] input[type='submit']");
                                bool keineButtonsMehr = await nachButton.CountAsync() == 0;

                                if (keineButtonsMehr)
                                {
                                    Console.WriteLine("[VERIFIZIERT] Zeiterfassung wurde beendet.");
                                    zeiterfassungGetriggert = false;
                                }
                                else
                                {
                                    Console.WriteLine("[WARN] Zeiterfassung scheint nicht beendet zu sein. Button zeigt weiter: " + (await nachButton.Nth(0).InnerTextAsync()).Trim());
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("[WARN] Buttontext ist weder Starten noch Beenden.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("[FEHLER IM MONITORING]");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("[INFO] Skript läuft weiter und prüft später erneut.");
                }

                await WaitNextAsync(pruefIntervall);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine(" KRITISCHER FEHLER");
            Console.WriteLine("========================================");
            Console.WriteLine(ex.ToString());
        }
    }

    static bool IsLoginPage(IPage page)
    {
        return page.Url.Contains("/login/", StringComparison.OrdinalIgnoreCase);
    }

    static async Task<IPage> GetOrCreatePageAsync(IBrowserContext context)
    {
        if (context.Pages.Count > 0)
        {
            return context.Pages[0];
        }
        var page = await context.NewPageAsync();
        AttachPageHandlers(page);
        return page;
    }

    static async Task<IPage> EnsurePageAsync(IBrowserContext context, IPage currentPage)
    {
        if (currentPage == null || currentPage.IsClosed)
        {
            Console.WriteLine("[WARN] Aktueller Tab ist geschlossen. Erzeuge neuen Tab.");
            var page = await GetOrCreatePageAsync(context);
            AttachPageHandlers(page);
            return page;
        }
        return currentPage;
    }

    static void AttachPageHandlers(IPage page)
    {
        page.Console += (_, msg) =>
        {
            Console.WriteLine($"[BROWSER] {msg.Type}: {msg.Text}");
        };

        page.PageError += (_, err) =>
        {
            Console.WriteLine($"[JS ERROR] {err}");
        };

        page.RequestFailed += (_, request) =>
        {
            Console.WriteLine($"[REQUEST FAILED] {request.Url}");
        };

        page.Dialog += async (_, dialog) =>
        {
            Console.WriteLine($"[DIALOG] {dialog.Message} (Typ: {dialog.Type})");
            await dialog.AcceptAsync();
            Console.WriteLine("[DIALOG] Dialog akzeptiert.");
        };
    }

    static async Task SafeGotoAsync(IPage page, string url)
    {
        try
        {
            await page.GotoAsync(
                url,
                new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 60000
                });

            Console.WriteLine($"[URL]   {page.Url}");
            Console.WriteLine($"[TITLE] {await page.TitleAsync()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[WARN] Navigation hatte ein Problem:");
            Console.WriteLine(ex.Message);
        }
    }

    static async Task<bool> TryClickLoginAsync(IPage page, bool dryRun)
    {
        try
        {
            var anmeldenButton = page.GetByRole(
                AriaRole.Button,
                new() { Name = "Anmelden" });

            if (await anmeldenButton.CountAsync() > 0)
            {
                if (!dryRun)
                {
                    await anmeldenButton.ClickAsync();
                }
                return true;
            }
        }
        catch
        {
            Console.WriteLine("[WARN] Login über Button-Text nicht möglich.");
        }

        try
        {
            var submit = page.Locator("button[type='submit'], input[type='submit']");
            if (await submit.CountAsync() > 0)
            {
                if (!dryRun)
                {
                    await submit.Nth(0).ClickAsync();
                }
                return true;
            }
        }
        catch
        {
            Console.WriteLine("[WARN] Login über Submit-Selector nicht möglich.");
        }

        return false;
    }

    static async Task TryOpenBlockDrawerAsync(IPage page)
    {
        try
        {
            var block = page.Locator("section[data-block='loginstat']");
            if (await block.CountAsync() == 0)
            {
                return;
            }

            bool blockVisible = await block.Nth(0).IsVisibleAsync();
            if (blockVisible)
            {
                Console.WriteLine("[OK] Zeiterfassung-Block ist sichtbar.");
                return;
            }

            Console.WriteLine("[INFO] Zeiterfassung-Block ist nicht sichtbar. Versuche Blockleiste zu öffnen ...");

            var drawerButton = page.Locator("button[data-toggler='drawers'][data-target='theme_boost-drawers-blocks']");
            if (await drawerButton.CountAsync() > 0)
            {
                await drawerButton.Nth(0).ClickAsync();
                Console.WriteLine("[OK] Blockleisten-Button geklickt.");
                await page.WaitForTimeoutAsync(1000);
            }
            else
            {
                Console.WriteLine("[WARN] Kein Blockleisten-Button gefunden.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[WARN] Blockleiste konnte nicht geprüft/geöffnet werden:");
            Console.WriteLine(ex.Message);
        }
    }

    static async Task TrySetArbeitsortAsync(IPage page, string? standort)
    {
        if (string.IsNullOrWhiteSpace(standort))
        {
            Console.WriteLine("[WARN] TrySetArbeitsortAsync aufgerufen mit leerem Standort.");
            return;
        }

        try
        {
            var form = page.Locator("section[data-block='loginstat'] form[action*='starten=1']");
            if (await form.CountAsync() == 0)
            {
                Console.WriteLine("[WARN] Startformular nicht gefunden. Arbeitsort kann nicht gesetzt werden.");
                return;
            }

            string desiredValue = standort.Equals("Homeoffice", StringComparison.OrdinalIgnoreCase) ? "1" : "2";

            Console.WriteLine($"[INFO] Setze Arbeitsort auf '{standort}' (value='{desiredValue}') ...");

            var radio = form.Locator($"input[type='radio'][name='homeo'][value='{desiredValue}']");
            int radioCount = await radio.CountAsync();

            if (radioCount == 0)
            {
                Console.WriteLine($"[WARN] Radio-Button für Arbeitsort nicht gefunden. Standort: {standort}, Value: {desiredValue}");
                return;
            }

            await radio.Nth(0).ClickAsync();
            await page.WaitForTimeoutAsync(300);

            bool isChecked = await radio.Nth(0).IsCheckedAsync();
            Console.WriteLine($"[OK] Arbeitsort gesetzt: {standort} (checked={isChecked})");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[WARN] Arbeitsort konnte nicht gesetzt werden:");
            Console.WriteLine(ex.Message);
        }
    }

    static async Task WaitNextAsync(TimeSpan interval)
    {
        Console.WriteLine();
        Console.WriteLine($"[INFO] Nächste Prüfung in {interval.TotalSeconds} Sekunden.");
        Console.WriteLine("[INFO] Q drücken zum Beenden.");
        await Task.Delay(interval);
    }

    static string? ErmittleArbeitsortAusWlan()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show interfaces",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return null;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var match = System.Text.RegularExpressions.Regex.Match(
                output,
                @"SSID\s*:\s*(.+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (match.Success)
            {
                string ssid = match.Groups[1].Value.Trim();
                Console.WriteLine($"[INFO] Aktuelles WLAN: {ssid}");
                return ssid.StartsWith("GFN", StringComparison.OrdinalIgnoreCase) ? "Standort" : "Homeoffice";
            }
        }
        catch { }

        return null;
    }

    static async Task<string?> TryGetTodaysAttendanceStatusAsync(IPage page, string anwesenheitUrl)
    {
        try
        {
            await SafeGotoAsync(page, anwesenheitUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
            await page.WaitForTimeoutAsync(2000);

            string heute = DateTime.Now.ToString("dd.MM.yyyy");
            
            var table = page.Locator("table.table.table-striped");
            int tableCount = await table.CountAsync();
            
            if (tableCount == 0)
            {
                return $"[STATUS] Heute ({heute}): Anwesenheitstabelle nicht gefunden.";
            }

            var rows = table.Locator("tbody tr");
            int rowCount = await rows.CountAsync();
            
            for (int i = 0; i < rowCount; i++)
            {
                var row = rows.Nth(i);
                var cells = row.Locator("td");
                int cellCount = await cells.CountAsync();
                
                if (cellCount >= 4)
                {
                    string datum = (await cells.Nth(0).InnerTextAsync()).Trim();
                    
                    if (datum == heute)
                    {
                        string standort = (await cells.Nth(1).InnerTextAsync()).Trim();
                        string loginzeit = (await cells.Nth(2).InnerTextAsync()).Trim();
                        string logoutzeit = (await cells.Nth(3).InnerTextAsync()).Trim();
                        
                        await SafeGotoAsync(page, GfnStartUrl);
                        return $"[ANWESENHEIT] {heute} | {standort} | Login: {loginzeit} | Logout: {logoutzeit}";
                    }
                }
            }

            await SafeGotoAsync(page, GfnStartUrl);
            return $"[STATUS] Heute ({heute}): Noch kein Eintrag in der Anwesenheitsliste.";
        }
        catch (Exception ex)
        {
            await SafeGotoAsync(page, GfnStartUrl);
            return "[WARN] Anwesenheitsstatus konnte nicht ermittelt werden: " + ex.Message;
        }
    }
}
