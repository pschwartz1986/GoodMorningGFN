<#
.SYNOPSIS
GoodMorningGFN Setup – Installieren oder Deinstallieren des Autostart-Tasks.
#>

param()

$ErrorActionPreference = 'Stop'

# ===========================
# Selbst-Admin-Rechte anfordern
# ===========================
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "powershell.exe"
    $psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    $psi.Verb = "runas"
    $psi.UseShellExecute = $true
    [System.Diagnostics.Process]::Start($psi) | Out-Null
    exit
}

# ===========================
# Konfiguration
# ===========================
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir  = Join-Path $projectDir 'publish'
$exePath    = Join-Path $publishDir 'GoodMorningGFN.exe'
$taskName   = 'GoodMorningGFN'
$playwrightVersion = '1.61.0'

# ===========================
# Menü
# ===========================
Clear-Host
Write-Host '========================================'
Write-Host '            GoodMorningGFN'
Write-Host '========================================'
Write-Host ''
Write-Host '  [1] Installieren (Autostart einrichten)'
Write-Host '  [2] Deinstallieren (Task entfernen)'
Write-Host ''
while ($true) {
    $wahl = Read-Host 'Auswahl'
    if ($wahl -eq '1') { $mode = 'install'; break }
    if ($wahl -eq '2') { $mode = 'uninstall'; break }
    Write-Host 'Ungültige Eingabe. Bitte 1 oder 2 eingeben.'
}

# ===========================
# Gemeinsame Hilfsfunktionen
# ===========================
function Test-Command {
    param([string]$Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Show-Status {
    param([string]$Label, [bool]$Ok)
    $symbol = if ($Ok) { '[OK]' } else { '[FEHLT]' }
    Write-Host "$symbol $Label"
}

function Test-IsAdmin {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal(
        [Security.Principal.WindowsIdentity]::GetCurrent()
    )
    return $currentPrincipal.IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator
    )
}

# ===========================
# Installations-Pfad
# ===========================
if ($mode -eq 'install') {

    Write-Host ''
    Write-Host '========================================'
    Write-Host ' INSTALLATION'
    Write-Host '========================================'
    Write-Host ''

    Write-Host '[HINWEIS] Autostart erfolgt uber den Windows Task Scheduler'
    Write-Host '         (Eintrag: Anmeldung / Systemstart).'
    Write-Host ''

    if (-not (Test-IsAdmin)) {
        Write-Host '[WARN] Skript ist NICHT als Administrator gestartet.'
        Write-Host '      Fur den Task Scheduler werden Admin-Rechte benotigt.'
        Write-Host ''
    }

    Write-Host '[1/7] Prüfe .NET SDK ...'
    if (Test-Command 'dotnet') {
        Show-Status ".NET SDK ($(dotnet --version))" $true
    }
    else {
        Write-Host '  -> .NET SDK 10 nicht gefunden. Installation wird gestartet ...'
        $installerUrl = 'https://dot.net/v1/dotnet-install.ps1'
        $installerPath = Join-Path $env:TEMP 'dotnet-install.ps1'
        Invoke-WebRequest -Uri $installerUrl -OutFile $installerPath
        & powershell -ExecutionPolicy Bypass -File $installerPath `
            -Channel 10.0 `
            -InstallDir 'C:\Program Files\dotnet' `
            -Quality GA
        $env:PATH = 'C:\Program Files\dotnet;' + $env:PATH
        if (-not (Test-Command 'dotnet')) {
            Write-Error '.NET SDK 10 konnte nicht installiert werden.'
            exit 1
        }
        Show-Status ".NET SDK ($(dotnet --version))" $true
    }

    Write-Host '[2/7] Prüfe dotnet-ef ...'
    $efOk = $true
    try {
        if (-not (Test-Command 'dotnet-ef')) {
            dotnet tool install --global dotnet-ef 2>&1 | Out-Null
        }
    }
    catch {
        $efOk = $false
    }
    Show-Status 'dotnet-ef' $efOk

    Write-Host '[3/7] Prüfe dotnet-watch ...'
    $watchOk = $true
    try {
        if (-not (Test-Command 'dotnet-watch')) {
            dotnet tool install --global dotnet-watch 2>&1 | Out-Null
        }
    }
    catch {
        $watchOk = $false
    }
    Show-Status 'dotnet-watch' $watchOk

    Write-Host '[4/7] Stelle NuGet-Pakete wieder her ...'
    $restoreOk = $true
    Push-Location $projectDir
    try {
        dotnet restore --disable-parallel 2>&1 | Out-Null
    }
    catch {
        $restoreOk = $false
    }
    Pop-Location
    Show-Status 'NuGet-Pakete' $restoreOk

    Write-Host '[5/7] Veröffentliche App (selbsthaltend, win-x64) ...'
    if (-not (Test-Path $publishDir)) {
        New-Item -ItemType Directory -Path $publishDir -Force | Out-Null | Out-Null
    }
    $publishOk = $true
    Push-Location $projectDir
    try {
        dotnet publish `
            -c Release `
            -r win-x64 `
            --self-contained true `
            -o $publishDir `
            /p:PublishSingleFile=false `
            /p:IncludeNativeLibrariesForSelfExtract=true `
            2>&1 | Out-Null
    }
    catch {
        $publishOk = $false
    }
    Pop-Location
    $exeExists = Test-Path $exePath
    Show-Status "Veröffentlicht: $exePath" $exeExists
    if (-not $exeExists) {
        Write-Error 'Publish fehlgeschlagen.'
        exit 1
    }

    Write-Host '[6/7] Installiere Playwright Browser (Chromium) ...'
    $playwrightOk = $false
    Push-Location $publishDir
    try {
        $playwrightCli = Join-Path $publishDir 'playwright.exe'
        if (-not (Test-Path $playwrightCli)) {
            Write-Host '  -> Playwright CLI als lokales Tool installieren ...'
            dotnet tool install Microsoft.Playwright.CLI `
                --version $playwrightVersion `
                --tool-path . `
                --ignore-failed-sources `
                2>&1 | Out-Null
        }
        if (Test-Path $playwrightCli) {
            Write-Host '  -> Installiere Chromium ...'
            & $playwrightCli install chromium 2>&1 | Out-Null
            $playwrightOk = $true
        }
    }
    catch {
    }
    Pop-Location
    Show-Status 'Playwright Chromium' $playwrightOk

    Write-Host '[7/7] Richte Autostart-Task ein ...'
    $trigger = "`"$exePath`""
    $deleted = $true
    try {
        schtasks.exe /Delete /TN $taskName /F 2>&1 | Out-Null
    }
    catch {
        $deleted = $false
    }
    try {
        schtasks.exe /Create /SC ONLOGON /TN $taskName /TR $trigger /F 2>&1 | Out-Null
    }
    catch {
    }
    $taskOk = $LASTEXITCODE -eq 0
    Show-Status "Geplante Aufgabe '$taskName'" $taskOk

    Write-Host ''
    Write-Host '========================================'
    Write-Host ' INSTALLATION ABGESCHLOSSEN'
    Write-Host '========================================'
    Write-Host ''
}

# ===========================
# Deinstallations-Pfad
# ===========================
if ($mode -eq 'uninstall') {

    Write-Host ''
    Write-Host '========================================'
    Write-Host ' DEINSTALLATION'
    Write-Host '========================================'
    Write-Host ''

    Write-Host '[1/2] Entferne geplante Aufgabe ...'

    $existsBefore = $false
    try {
        $null = schtasks.exe /Query /TN $taskName 2>&1 | Out-Null
        $existsBefore = $LASTEXITCODE -eq 0
    }
    catch {
        $existsBefore = $false
    }

    if (-not $existsBefore) {
        Show-Status "Aufgabe '$taskName' existiert nicht" $false
    }
    else {
        $deleted = $false
        try {
            schtasks.exe /Delete /TN $taskName /F 2>&1 | Out-Null
            $deleted = $LASTEXITCODE -eq 0
        }
        catch {
            $deleted = $false
        }
        Show-Status "Aufgabe '$taskName' geloscht" $deleted

        if (-not $deleted) {
            Write-Error 'Aufgabe konnte nicht geloscht werden. Bitte als Administrator ausfuhren.'
        }
    }

    Write-Host '[2/2] Prufe, ob Aufage wirklich entfernt wurde ...'
    $stillExists = $false
    try {
        $null = schtasks.exe /Query /TN $taskName 2>&1 | Out-Null
        $stillExists = $LASTEXITCODE -eq 0
    }
    catch {
        $stillExists = $false
    }

    if ($stillExists) {
        Write-Host ''
        Write-Host '------------------------------------------------'
        Write-Host " FEHLER: Aufgabe '$taskName' existiert weiterhin."
        Write-Host '------------------------------------------------'
        Write-Host 'Haufige Ursache:'
        Write-Host '  - Skript nicht als Administrator gestartet.'
        Write-Host '  - UAC-Blockierung.'
        Write-Host ''
    }
    else {
        Show-Status "Aufgabe '$taskName' ist NICHT mehr vorhanden" $true
    }

    Write-Host ''
    Write-Host '========================================'
    Write-Host ' DEINSTALLATION ABGESCHLOSSEN'
    Write-Host '========================================'
    Write-Host ''
}

# ===========================
# Abschluss-Status
# ===========================
Write-Host '========================================'
Write-Host ' ZUSTANDS-PRÜFUNG'
Write-Host '========================================'
Write-Host ''

if ($mode -eq 'install') {
    Write-Host " Programm:   $exePath"
    Write-Host " Vorhanden:  $(if (Test-Path $exePath) { 'JA' } else { 'NEIN' })"
    Write-Host ''
    Write-Host ' Hinweis: Autostart erfolgt uber den Windows Task Scheduler,'
    Write-Host '          nicht uber den Autostart-Ordner (shell:startup).'
    Write-Host ''
    Write-Host " Task:       $taskName"
    $query = schtasks.exe /Query /TN $taskName /FO LIST 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host ' Aufgabe:    VORHANDEN'
        Write-Host ' Trigger:    Anmeldung / Systemstart (ONLOGON)'
        Write-Host ''
        Write-Host $query
    }
    else {
        Write-Host ' Aufgabe:    FEHLT'
        Write-Host ''
        Write-Host 'Hinweis: Eventuell fehlen Administrator-Rechte.'
        Write-Host ''
    }
}

if ($mode -eq 'uninstall') {
    $stillExists = $false
    try {
        $null = schtasks.exe /Query /TN $taskName 2>&1 | Out-Null
        $stillExists = $LASTEXITCODE -eq 0
    }
    catch {
        $stillExists = $false
    }

    Write-Host " Task:        $taskName"
    Write-Host " Vorhanden:   $(if ($stillExists) { 'JA (FEHLER)' } else { 'NEIN (OK)' })"
    Write-Host ''
}

Write-Host ''
Write-Host '========================================'
Write-Host ' FERTIG'
Write-Host '========================================'
