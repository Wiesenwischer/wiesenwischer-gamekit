# Build Pipeline, Launcher & Auto-Update — Engineering Specification

Version: 1.0 (Konsolidiert)
Zielgruppe: Core Dev + Tester (Sohn/Freunde)
Priorität: Stabilität, reproduzierbare Builds, einfacher Distribution-Flow

---

## 0. Zielbild

### Anforderungen

- **CI Build** nach Merge auf `main` (GitHub Actions)
- **Artefakt-Hosting** über GitHub Releases inkl. Version-Metadaten
- **Launcher** (separate WPF/.NET App) lädt Updates, entpackt, startet Spiel
- **Channels** (mind. `dev` und `stable`)
- **Optional**: Delta/Patch später möglich, initial Full ZIP Updates ausreichend
- **Sicherheit**: Hash/Signaturprüfung gegen Korruption / Manipulation
- **Erweiterbar**: Telemetrie, Crash Reporting, CDN, Rollback

### Technologieentscheidungen

- Repository: GitHub
- CI/CD: GitHub Actions
- Storage: GitHub Releases (optional später: externe CDN/Storage)
- Launcher: .NET 8/9 + WPF (Windows)

---

## 1. Architekturübersicht

### 1.1 Komponenten

1) **GitHub Repository** (Unity Projekt)
2) **GitHub Actions Workflow** (CI/CD Pipeline)
3) **GitHub Releases** (Artifact Storage, Manifest + ZIP)
4) **Manifest** (`manifest.json` — Version + Hashes + URL pro Channel)
5) **Launcher** (.NET 8/9 + WPF)
6) **Game Install** (Ordnerstruktur, Version-Datei, Content)

### 1.2 High-Level Flow

```
Git Push / Merge → main
        ↓
GitHub Action startet
        ↓
Unity Headless Build
        ↓
ZIP + Hash erzeugen + manifest.json generieren
        ↓
GitHub Release Upload (ZIP + manifest.json)
        ↓
Launcher (Client):
  - reads local manifest/version
  - downloads remote manifest
  - compares versions
  - downloads ZIP if newer
  - verifies SHA-256
  - extracts to staging
  - swaps atomically
  - starts Game.exe
```

---

## 2. Repository-Struktur (CI-relevant)

Empfehlung:

```
/.github/workflows/
    build.yml

/build/
    BuildScript.cs
    (weitere Build-bezogene Dateien)

/Launcher/
    (separates .NET Projekt oder eigenes Repo)
```

---

## 3. Artifact Hosting & Manifest Design

### 3.1 GitHub Releases Struktur

Jedes Release enthält:
- `build_<version>.zip` — der gepackte Game Build
- `manifest.json` — Version + Download-Metadaten

Channels werden über Tags/Branches gesteuert:
- `main` Push → `dev` Channel (automatisch)
- `v*` Tag → `stable` Channel (manuell promotet)

### 3.2 Manifest (Minimal, aber robust)

`manifest.json` (pro Channel + Plattform):

```json
{
  "appId": "com.yourstudio.mmo",
  "channel": "dev",
  "platform": "windows-x64",
  "version": "1.0.43",
  "buildId": "2026.02.16.1",
  "publishedAtUtc": "2026-02-16T20:45:00Z",
  "download": {
    "url": "https://github.com/<owner>/<repo>/releases/download/<tag>/build_1.0.43.zip",
    "sizeBytes": 123456789,
    "sha256": "BASE64_OR_HEX",
    "sig": "OPTIONAL_SIGNATURE_BASE64"
  },
  "launch": {
    "exe": "Game/MyMMO.exe",
    "args": ""
  },
  "minLauncherVersion": "1.0.0",
  "notes": "Optional release notes"
}
```

**Begründung der Felder:**
- `version`: SemVer/BuildNum, zentral für Vergleich
- `sha256`: Integrity Check (unbedingt!)
- `sig`: optionale Signatur (später: RSA/ECDSA)
- `minLauncherVersion`: ermöglicht Launcher-Upgrades ohne Bruch
- `launch.exe`: entkoppelt Launcher von hartcodiertem Pfad
- `buildId`: CI Build Nummer / Timestamp für eindeutige Identifikation
- `publishedAtUtc`: Zeitstempel für Sortierung/Anzeige
- `notes`: Release Notes für den Launcher

### 3.3 Versionierungsempfehlung

- **Game Version**: `Major.Minor.Patch` oder `Major.Minor.Build`
- **BuildId**: GitHub Run Number oder Timestamp
- `dev` Channel kann jede Merge-Version haben, `stable` nur Tags/Manual Promote
- Version aus: Git tag / branch / `github.run_number`

Beispiel:
```
version = 1.0.${{ github.run_number }}
```

---

## 4. CI/CD Pipeline — GitHub Actions Spezifikation

### 4.1 Trigger

- `main` Push: Build + Publish `dev` Channel
- `v*` Tag: Build + Publish `stable` Channel
- Optional: PR Build nur als Artifact, nicht publizieren

```yaml
on:
  push:
    branches: [ main ]
  tags:
    - 'v*'
```

### 4.2 Build Agent / Runner

GitHub-hosted oder self-hosted Windows Runner.

Installiert:
- Unity Editor (passende Version, z.B. 2022 LTS / 6000 LTS)
- Unity Build Support (Windows + optional weitere Plattformen)
- .NET SDK (für Launcher Build optional)

Empfohlen: **game-ci/unity-builder** Action für vereinfachtes Unity-Setup.

```yaml
- uses: game-ci/unity-builder@v4
  with:
    targetPlatform: StandaloneWindows64
```

### 4.3 Pipeline Steps (detailliert)

#### Step A — Checkout

- Checkout Repo (mit LFS falls benutzt)

```yaml
- uses: actions/checkout@v4
  with:
    fetchDepth: 0
    lfs: true
```

#### Step B — Restore/Preparation

- `Library/` Cache optional (beschleunigt Builds erheblich)
- Unity Lizenz aktivieren (Unity ULF/License je nach Setup)

```yaml
- uses: actions/cache@v4
  with:
    path: Library
    key: Library-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
    restore-keys: Library-
```

#### Step C — Compute Version

- Version aus Git tag / branch / build number
- Speichere Version als Environment Variable `GAME_VERSION`

```yaml
- name: Compute version
  run: |
    echo "GAME_VERSION=1.0.${{ github.run_number }}" >> $GITHUB_ENV
```

#### Step D — Unity Headless Build

Entweder via game-ci/unity-builder oder manuell:

```yaml
- uses: game-ci/unity-builder@v4
  with:
    targetPlatform: StandaloneWindows64
    buildMethod: BuildScript.BuildWindows
```

Alternativ manuell (self-hosted):
```
Unity.exe -batchmode -quit -nographics \
  -projectPath "." \
  -executeMethod BuildScript.BuildWindows \
  -logFile "unity.log"
```

**BuildScript Anforderungen:**
- Muss ExitCode sauber setzen (throw Exception bei Fehler)
- Output Directory konfigurierbar
- Version aus Environment Variable oder Kommandozeilen-Argument lesen

#### Step E — Package (ZIP)

Ordnerstruktur im ZIP:
```
/Game/...
```

Empfehlung: ZIP enthält nur Game-Ordner, Launcher ist separat.

```yaml
- name: Package ZIP
  run: |
    Compress-Archive -Path "build/*" -DestinationPath "build_${{ env.GAME_VERSION }}.zip" -Force
  shell: pwsh
```

#### Step F — Hash berechnen

SHA-256 des ZIPs berechnen + Größe in Bytes bestimmen:

```yaml
- name: Compute SHA256
  run: |
    $zip = "build_${{ env.GAME_VERSION }}.zip"
    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()
    $size = (Get-Item $zip).Length
    echo "ZIP_SHA256=$hash" >> $env:GITHUB_ENV
    echo "ZIP_SIZE=$size" >> $env:GITHUB_ENV
  shell: pwsh
```

#### Step G — Manifest generieren

Erzeuge `manifest.json` mit Version, BuildId, PublishedAtUtc, Download-URL, Hash:

```yaml
- name: Generate manifest.json
  run: |
    $ver = "${{ env.GAME_VERSION }}"
    $channel = "dev"  # oder aus Tag ableiten
    $tag = "v$ver"
    $url = "https://github.com/${{ github.repository }}/releases/download/$tag/build_$ver.zip"
    $manifest = @{
      appId = "com.yourstudio.mmo"
      channel = $channel
      platform = "windows-x64"
      version = $ver
      buildId = "${{ github.run_id }}"
      publishedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
      download = @{
        url = $url
        sizeBytes = [int64]"${{ env.ZIP_SIZE }}"
        sha256 = "${{ env.ZIP_SHA256 }}"
      }
      launch = @{ exe = "Game/MyMMO.exe"; args = "" }
      minLauncherVersion = "1.0.0"
    } | ConvertTo-Json -Depth 10
    $manifest | Out-File -Encoding utf8 "manifest.json"
  shell: pwsh
```

#### Step H — GitHub Release Upload

ZIP und manifest.json als Release Assets hochladen:

```yaml
- uses: softprops/action-gh-release@v2
  with:
    tag_name: v${{ env.GAME_VERSION }}
    files: |
      build_${{ env.GAME_VERSION }}.zip
      manifest.json
```

**Wichtig**: Upload-Reihenfolge: 1) ZIP hochladen 2) manifest.json zuletzt hochladen (damit Clients nicht auf fehlende ZIP zeigen). Bei GitHub Releases wird dies automatisch atomar gehandhabt.

#### Step I — Pipeline Artifacts publizieren

Optional: Unity Log und Build-Artefakte als Workflow Artifacts speichern (für Debugging):

```yaml
- uses: actions/upload-artifact@v4
  if: always()
  with:
    name: build-log
    path: unity.log
```

### 4.4 GitHub Actions Workflow Skeleton

> Hinweis: Dies ist ein **Skeleton**. Unity-Pfade, Secrets und Projekt-spezifische Werte müssen angepasst werden.

```yaml
name: Build & Release

on:
  push:
    branches: [ main ]
    tags: [ 'v*' ]

jobs:
  build:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v4
      with:
        fetchDepth: 0
        lfs: true

    - uses: actions/cache@v4
      with:
        path: Library
        key: Library-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
        restore-keys: Library-

    - name: Compute version
      run: echo "GAME_VERSION=1.0.${{ github.run_number }}" >> $GITHUB_ENV

    - uses: game-ci/unity-builder@v4
      with:
        targetPlatform: StandaloneWindows64

    - name: Package ZIP
      run: |
        Compress-Archive -Path "build/*" -DestinationPath "build_${{ env.GAME_VERSION }}.zip" -Force
      shell: pwsh

    - name: Compute SHA256
      run: |
        $zip = "build_${{ env.GAME_VERSION }}.zip"
        $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()
        $size = (Get-Item $zip).Length
        echo "ZIP_SHA256=$hash" >> $env:GITHUB_ENV
        echo "ZIP_SIZE=$size" >> $env:GITHUB_ENV
      shell: pwsh

    - name: Generate manifest.json
      run: |
        $ver = "${{ env.GAME_VERSION }}"
        $tag = "v$ver"
        $url = "https://github.com/${{ github.repository }}/releases/download/$tag/build_$ver.zip"
        $manifest = @{
          appId = "com.yourstudio.mmo"
          channel = "dev"
          platform = "windows-x64"
          version = $ver
          buildId = "${{ github.run_id }}"
          publishedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
          download = @{
            url = $url
            sizeBytes = [int64]"${{ env.ZIP_SIZE }}"
            sha256 = "${{ env.ZIP_SHA256 }}"
          }
          launch = @{ exe = "Game/MyMMO.exe"; args = "" }
          minLauncherVersion = "1.0.0"
        } | ConvertTo-Json -Depth 10
        $manifest | Out-File -Encoding utf8 "manifest.json"
      shell: pwsh

    - uses: softprops/action-gh-release@v2
      with:
        tag_name: v${{ env.GAME_VERSION }}
        files: |
          build_${{ env.GAME_VERSION }}.zip
          manifest.json

    - uses: actions/upload-artifact@v4
      if: always()
      with:
        name: build-log
        path: unity.log
```

### 4.5 Secrets & Access

- GitHub Token wird automatisch bereitgestellt (`GITHUB_TOKEN`)
- Unity Lizenz als Repository Secret (`UNITY_LICENSE`)
- Tokens nie im Repo speichern
- GitHub Releases sind per Default public (public repo) oder private (private repo)

---

## 5. Launcher — Spezifikation

### 5.1 Tech Stack

- **.NET 8/9 + WPF** (Windows)
- Async/await, HttpClient
- ZIP Extraction: `System.IO.Compression.ZipFile` (oder SharpZipLib falls nötig)

### 5.2 Installer vs Portable

Einfachste Version:
- Launcher ist portable (ZIP/Installer egal)
- Launcher verwaltet Game Installation im Unterordner

### 5.3 Install-Struktur

```
%LOCALAPPDATA%\YourStudio\MyMMO\
  Launcher\
  Game\
  Staging\
  Cache\
  Backup\
  local.json
  logs\
```

### 5.4 Channel Auswahl

- `dev`, `stable`
- Speicherung in `local.json`
- UI Toggle im Launcher

### 5.5 Local State Datei

`local.json`:

```json
{
  "channel": "dev",
  "installedVersion": "1.0.42",
  "installPath": "C:\\Users\\...\\MyMMO",
  "lastUpdateUtc": "2026-02-16T20:00:00Z"
}
```

---

## 6. Launcher Startup Flow (detailliert)

1) Load `local.json`
2) Fetch remote `manifest.json` (channel, platform) von GitHub Releases
3) Compare versions (SemVer compare)
4) If update needed:
   - Download ZIP to Cache
   - Verify SHA-256 (and signature optional)
   - Extract to `Staging/<version>/`
   - Swap:
     - Ensure Game nicht laufend
     - Move current `Game/` → `Backup/Game_<oldversion>/` (optional)
     - Move `Staging/<version>/Game/` → `Game/`
     - Delete staging
   - Update `local.json`
5) Start Game.exe
6) Monitor Game process (optional)
7) Cleanup old builds (policy)

### 6.1 Version Vergleich

Empfehlung: SemVer compare. Falls nur numerische Buildnummern: robust string/numeric compare implementieren.

---

## 7. Updater Implementation — Details

### 7.1 Download (resumable optional)

- Initial: einfacher Download mit Progress
- Später: Range Requests für Resume

**HttpClient**: single static HttpClient, stream to file, progress via content-length

```csharp
using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
resp.EnsureSuccessStatusCode();
var total = resp.Content.Headers.ContentLength ?? -1;
using var input = await resp.Content.ReadAsStreamAsync();
using var output = File.Create(tmpFile);
// Stream mit Progress-Tracking
var buffer = new byte[81920];
long downloaded = 0;
int bytesRead;
while ((bytesRead = await input.ReadAsync(buffer)) > 0)
{
    await output.WriteAsync(buffer.AsMemory(0, bytesRead));
    downloaded += bytesRead;
    ReportProgress(downloaded, total);
}
```

### 7.2 Integrity Check (MUSS)

SHA-256 berechnen und mit Manifest vergleichen:

```csharp
using var stream = File.OpenRead(file);
using var sha256 = SHA256.Create();
var hashBytes = sha256.ComputeHash(stream);
var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
if (!hash.Equals(manifest.Download.Sha256, StringComparison.OrdinalIgnoreCase))
    throw new InvalidDataException("Hash mismatch — download corrupt or tampered");
```

### 7.3 Extraction (Staging)

- **Nie** direkt in `Game/` entpacken
- Immer staging → dann swap
- So ist Update **atomar** und rollbackfähig

```
Cache/build_1.0.43.zip  →  Staging/1.0.43/Game/...
```

### 7.4 Atomic Swap Strategie

Windows: Directory Move ist schnell/atomar genug, wenn im gleichen Volume.

Algorithmus:
1) Ensure Game not running (check process)
2) `Game/` → `Backup/Game_<oldversion>/` (optional)
3) `Staging/<version>/Game/` → `Game/`
4) Delete staging
5) Update `local.json`

Wenn Move fehlschlägt: Rollback via Backup

### 7.5 File Locks vermeiden

- Launcher startet Game erst **nach** Update
- Launcher darf nicht im `Game/` Ordner liegen (sonst self-update schwierig)

### 7.6 Launcher Self-Update (optional)

Später via `minLauncherVersion` + separatem Launcher-Manifest. Initial nicht nötig, aber durch `minLauncherVersion`-Feld vorbereitet.

---

## 8. Launcher UI — Anforderungen

### 8.1 Screens / Bereiche

1) **Channel Auswahl** (`dev`/`stable`) — Dropdown oder Toggle
2) **Update Status** (Check, Download, Verify, Extract) — Statustext
3) **Progressbar** + Speed + ETA optional
4) **Play Button** — deaktiviert während Update
5) **Logs / Error Details** — einfacher Expand-Bereich

### 8.2 UX Verhalten

- Auto-Check on Start
- Bei Update: Play-Button deaktivieren bis fertig
- On Failure: "Retry" + "Open Logs" anzeigen
- Channel-Wechsel: sofort prüfen ob andere Version verfügbar

---

## 9. Security & Trust

### 9.1 Minimum (Pflicht)

- SHA-256 Hash in Manifest (Integrity Check)
- HTTPS only (GitHub Releases ist automatisch HTTPS)
- Manifest und ZIP aus kontrollierter Quelle (eigenes Repo/Releases)

### 9.2 Optional (sehr empfehlenswert später)

- Signiere Manifest oder ZIP (RSA/ECDSA)
- Public Key im Launcher eingebettet
- Verhindert Manipulation auch bei kompromittiertem CDN/Storage
- `sig` Feld im Manifest ist dafür vorbereitet

---

## 10. Multi-Platform (Optional Roadmap)

- Windows: WPF Launcher (primär)
- macOS: Swift/MAUI/Qt Launcher
- Linux: .NET + Avalonia

Pipeline dann pro Plattform getrennt mit Matrix-Build in GitHub Actions.

---

## 11. Betrieb & Wartung

### 11.1 Retention Policy

- Storage behält z.B. letzte 20 dev Releases
- stable Releases: alle behalten
- Alte Releases können manuell oder per GitHub Action aufgeräumt werden

### 11.2 Rollback

- manifest.json zurück auf alte Version setzen (altes Release als "latest" markieren)
- Clients holen automatisch die dort referenzierte Version

### 11.3 Telemetrie (optional)

- Launcher sendet anonym:
  - Current version
  - Update success/failure
  - Download time / speed

---

## 12. BuildScript — Unity Editor Script

### 12.1 Anforderungen

- Statische Methode `BuildWindows()` für Headless-Aufruf
- Konfiguriertes Output-Directory
- Sauberer ExitCode (Exception bei Fehler → Unity gibt non-zero zurück)
- Szenen-Liste aus Build Settings oder explizit definiert

### 12.2 Beispiel-Skeleton

```csharp
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    public static void BuildWindows()
    {
        var scenes = new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Gameplay.unity"
        };

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "build/Game/MyMMO.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Build failed: {report.summary.totalErrors} errors");
            throw new System.Exception("Build failed");
        }

        Debug.Log($"Build succeeded: {report.summary.outputPath}");
    }
}
```

---

## 13. Implementation Checklist

### Pipeline

- [ ] Unity BuildScript (`BuildScript.cs`)
- [ ] GitHub Actions Workflow (`build.yml`)
- [ ] ZIP Packaging
- [ ] SHA-256 Hash Generation
- [ ] Manifest Generation
- [ ] GitHub Release Upload (ZIP + manifest)
- [ ] Channel Logic dev/stable (Branch vs Tag)
- [ ] Unity Log als Workflow Artifact
- [ ] Library Cache

### Launcher

- [ ] .NET Projekt-Struktur (WPF)
- [ ] Local State File (`local.json`)
- [ ] Manifest Download (von GitHub Releases)
- [ ] Version Compare (SemVer)
- [ ] ZIP Download to Cache (mit Progress)
- [ ] SHA-256 Hash Verify
- [ ] Extract to Staging
- [ ] Atomic Swap + Rollback
- [ ] Start Game (.exe)
- [ ] UI: Progress Bar + Status
- [ ] UI: Play Button + Channel Toggle
- [ ] UI: Error Display + Retry
- [ ] Logging

---

## 14. Implementierungsreihenfolge (Empfohlen)

1) **BuildScript** — Unity headless build konfigurieren
2) **GitHub Actions Workflow** — Build → ZIP → Hash → Manifest → Release Upload
3) **Launcher Projekt-Struktur** — .NET 8 WPF Projekt anlegen
4) **Launcher Core** — Manifest fetch + version compare + download + verify + extract + swap + start
5) **Channel Support** (dev/stable)
6) **Launcher UI** — Progress, Play, Errors, Logs
7) **Polish** — Robust error handling, retry, logging
8) **Optional** — Signature, Launcher self-update, Telemetrie, Delta Updates

---

END OF SPEC
