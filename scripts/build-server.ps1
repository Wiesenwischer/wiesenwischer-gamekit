<#
.SYNOPSIS
    Baut den Linux Dedicated Server und optional einen Docker-Container.

.DESCRIPTION
    Workflow: Unity CLI Build → Docker Build → Docker Compose Up

.PARAMETER UnityPath
    Pfad zur Unity Editor Executable.

.PARAMETER SkipBuild
    Unity-Build ueberspringen (vorhandenen Build-Output nutzen).

.PARAMETER DockerBuild
    Docker-Image nach dem Unity-Build bauen.

.PARAMETER DockerRun
    Docker-Container nach dem Build starten.

.EXAMPLE
    # Nur Unity Server Build
    .\build-server.ps1

    # Alles: Build + Docker Image + Container starten
    .\build-server.ps1 -DockerBuild -DockerRun

    # Docker neu bauen ohne Unity-Build
    .\build-server.ps1 -SkipBuild -DockerBuild -DockerRun
#>
param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe",
    [switch]$SkipBuild,
    [switch]$DockerBuild,
    [switch]$DockerRun
)

$ErrorActionPreference = "Stop"
$ProjectPath = Split-Path -Parent $PSScriptRoot
$LogFile = Join-Path $ProjectPath "Logs\server-build.log"

Write-Host "=== Wiesenwischer GameKit - Server Build ===" -ForegroundColor Cyan
Write-Host "Project: $ProjectPath"

# --- Step 1: Unity Build ---
if (-not $SkipBuild) {
    Write-Host "`n[1/3] Building Linux Server..." -ForegroundColor Yellow

    if (-not (Test-Path $UnityPath)) {
        Write-Error "Unity nicht gefunden: $UnityPath`nBitte -UnityPath angeben oder Unity Hub pruefen."
        exit 1
    }

    # Log-Verzeichnis sicherstellen
    $logDir = Split-Path $LogFile -Parent
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }

    $buildArgs = @(
        "-batchmode",
        "-quit",
        "-nographics",
        "-projectPath", $ProjectPath,
        "-executeMethod", "BuildScript.BuildLinuxServer",
        "-logFile", $LogFile,
        "-buildTarget", "Linux64"
    )

    Write-Host "Unity: $UnityPath"
    Write-Host "Args: $($buildArgs -join ' ')"
    Write-Host "Log: $LogFile"
    Write-Host ""
    Write-Host "Build laeuft... (kann mehrere Minuten dauern)" -ForegroundColor DarkYellow

    $process = Start-Process -FilePath $UnityPath -ArgumentList $buildArgs -Wait -PassThru -NoNewWindow

    if ($process.ExitCode -ne 0) {
        Write-Host "`n--- Letzte 30 Zeilen Build-Log ---" -ForegroundColor Red
        if (Test-Path $LogFile) {
            Get-Content $LogFile -Tail 30
        }
        Write-Error "Unity Build fehlgeschlagen (Exit Code: $($process.ExitCode)). Siehe Log: $LogFile"
        exit 1
    }

    Write-Host "Unity Build erfolgreich!" -ForegroundColor Green
} else {
    Write-Host "`n[1/3] Unity Build uebersprungen (-SkipBuild)" -ForegroundColor DarkYellow
}

# --- Verify Build Output ---
$serverBinary = Join-Path $ProjectPath "Builds\Server\GameKit_HDRP"
if (-not (Test-Path $serverBinary)) {
    Write-Error "Server-Binary nicht gefunden: $serverBinary`nBitte zuerst ohne -SkipBuild ausfuehren."
    exit 1
}

$buildSize = (Get-ChildItem (Join-Path $ProjectPath "Builds\Server") -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "Build-Groesse: $([math]::Round($buildSize, 1)) MB"

# --- Step 2: Docker Build ---
if ($DockerBuild -or $DockerRun) {
    Write-Host "`n[2/3] Building Docker Image..." -ForegroundColor Yellow

    Push-Location $ProjectPath
    try {
        docker build -t wiesenwischer-gameserver:latest .
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Docker Build fehlgeschlagen!"
            exit 1
        }
    } finally {
        Pop-Location
    }

    Write-Host "Docker Image gebaut: wiesenwischer-gameserver:latest" -ForegroundColor Green
} else {
    Write-Host "`n[2/3] Docker Build uebersprungen" -ForegroundColor DarkYellow
}

# --- Step 3: Docker Run ---
if ($DockerRun) {
    Write-Host "`n[3/3] Starte Docker Container..." -ForegroundColor Yellow

    # Bestehenden Container stoppen falls vorhanden (Fehler ignorieren beim ersten Start)
    $ErrorActionPreference = "SilentlyContinue"
    docker stop wiesenwischer-gameserver 2>&1 | Out-Null
    docker rm wiesenwischer-gameserver 2>&1 | Out-Null
    $ErrorActionPreference = "Stop"

    Push-Location $ProjectPath
    try {
        docker compose up -d
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Docker Compose fehlgeschlagen!"
            exit 1
        }
    } finally {
        Pop-Location
    }

    Write-Host ""
    Write-Host "Server gestartet!" -ForegroundColor Green
    Write-Host "  Verbinden: localhost:7770 (UDP)"
    Write-Host "  Logs:      docker logs -f wiesenwischer-gameserver"
    Write-Host "  Stoppen:   docker compose down"
} else {
    Write-Host "`n[3/3] Docker Start uebersprungen" -ForegroundColor DarkYellow
}

Write-Host "`n=== Fertig ===" -ForegroundColor Cyan
