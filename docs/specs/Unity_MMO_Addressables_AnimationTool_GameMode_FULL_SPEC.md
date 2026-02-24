# Unity MMO -- Addressables, Animation Tool Integration & GameMode Architecture

## FULL ENGINEERING SPEC (Runtime Tools + Patch Architecture)

Version: 1.0\
Ziel: Direkt umsetzbare technische Spezifikation für:

-   Addressables Patch Architektur
-   Integration eines Runtime Animation Tuning Tools
-   Tool Scene vs Game Scene Struktur
-   GameMode Architektur
-   Stabilität und saubere Trennung zwischen Gameplay und Tooling

Diese Spec ist bewusst **engineering-lastig** geschrieben, sodass du
direkt mit der Implementierung beginnen kannst.

------------------------------------------------------------------------

# 1. Gesamtarchitektur Überblick

Das System wird in mehrere klar getrennte Layer aufgeteilt:

    Launcher
       |
    Core Game Build (Unity Player)
       |
    GameMode System
       |
    Scenes:
        - Game Scene
        - Animation Tool Scene
       |
    Addressables Remote Content

Ziele:

-   Gameplay bleibt stabil.
-   Tooling beeinflusst Gameplay nicht direkt.
-   Content kann gepatcht werden ohne Full Build.

------------------------------------------------------------------------

# 2. Addressables -- Technisches Grundprinzip

## 2.1 Was Addressables tun

Addressables erlauben:

-   Assets außerhalb des Builds zu speichern.
-   Assets remote zu laden.
-   Updates ohne neuen Full Game Build.

Unity erzeugt:

    catalog.json
    bundle_x.bundle
    bundle_y.bundle

Catalog mappt:

    Address → Bundle → Asset

------------------------------------------------------------------------

## 2.2 Warum Addressables hier wichtig sind

Dein Setup:

-   Animation Tool
-   Combat Tweaks
-   Iteration durch Tester

Ohne Addressables:

-   jeder kleine Asset Change = Full Build.

Mit Addressables:

-   nur Bundle neu bauen
-   Upload
-   Game lädt Patch automatisch.

------------------------------------------------------------------------

# 3. Addressables Setup (Schritt für Schritt)

## 3.1 Package installieren

Unity:

    Window → Package Manager
    → Addressables

------------------------------------------------------------------------

## 3.2 Addressables Fenster

    Window → Asset Management → Addressables → Groups

------------------------------------------------------------------------

## 3.3 Asset Addressable machen

Asset auswählen:

Inspector:

    [ ] Addressable

Adresse setzen:

    SwordAttack

------------------------------------------------------------------------

## 3.4 Remote Content konfigurieren

Addressables Groups → Profiles:

Setze:

    RemoteBuildPath
    RemoteLoadPath

Beispiel:

    RemoteLoadPath =
    https://cdn.example.com/content/[BuildTarget]

------------------------------------------------------------------------

## 3.5 Group Remote setzen

Group auswählen:

    Build Path → RemoteBuildPath
    Load Path → RemoteLoadPath

------------------------------------------------------------------------

## 3.6 Build Addressables

    Build → New Build → Default Build Script

Unity erstellt:

    ServerData/
       catalog.json
       bundles/

Diese Dateien werden nach Cloudflare hochgeladen.

------------------------------------------------------------------------

# 4. Hybrid Addressables Strategie (WICHTIG)

Nicht alles Addressable machen.

## Core Build enthält:

-   Player Controller
-   Core Systems
-   Main UI
-   Basale Animationen

## Addressables enthalten:

-   Combat tuning assets
-   Animation Variants
-   FX
-   Audio
-   Tool Assets (optional)

------------------------------------------------------------------------

# 5. Animation Tool Integration Architektur

## 5.1 Tool als Scene (empfohlen)

Tool wird als eigene Scene gebaut:

    Assets/Scenes/CombatToolScene.unity

Warum Scene?

-   eigenständige Umgebung
-   keine Vermischung mit Gameplay
-   leichter Debug Flow

------------------------------------------------------------------------

## 5.2 Tool Scene Inhalt

    CombatToolRoot
        PlayerPreview
        EnemyDummy
        TimelineUI
        ToolCamera

------------------------------------------------------------------------

## 5.3 Tool Funktionalität

-   Animation Scrubbing
-   Frame Timeline
-   Hit Window Editing
-   Preview Combat Simulation

------------------------------------------------------------------------

## 5.4 Animation Playback (Preview)

Scrubbing:

    animator.Play("Attack", 0, normalizedTime);
    animator.Update(0);

------------------------------------------------------------------------

## 5.5 Live Play Mode

    combatController.StartAttack(attackDefinition);

------------------------------------------------------------------------

# 6. GameMode Architektur (SEHR WICHTIG)

Problem:

Gameplay Scene und Tool Scene brauchen völlig unterschiedliche Systeme.

Lösung:

GameMode Pattern.

------------------------------------------------------------------------

## 6.1 GameMode Interface

    interface IGameMode
    {
        void Enter();
        void Exit();
    }

------------------------------------------------------------------------

## 6.2 GameModes

    MMOGameMode
    CombatToolGameMode

------------------------------------------------------------------------

## MMOGameMode

-   Network Systems
-   AI
-   Full gameplay loop

------------------------------------------------------------------------

## CombatToolGameMode

-   Debug systems
-   Fixed camera
-   Dummy enemy logic
-   Tool UI

------------------------------------------------------------------------

## 6.3 GameMode Manager

    GameModeManager
        SetGameMode(mode)

------------------------------------------------------------------------

Pseudo:

    if(commandLine == "-tool")
        SetGameMode(CombatToolMode);
    else
        SetGameMode(MMOGameMode);

------------------------------------------------------------------------

# 7. Tool Scene Launch Optionen

## Option A -- Developer Menu

Main Menu → Developer → Animation Tool.

------------------------------------------------------------------------

## Option B -- Command Line

    MyGame.exe -tool combat

------------------------------------------------------------------------

# 8. Addressables + Tool Integration

Optional:

Tool Scene selbst Addressable machen.

Vorteile:

-   nicht im Core Build
-   optionaler Download

------------------------------------------------------------------------

# 9. Patch Flow Gesamt

Developer:

-   Animation ändern
-   Addressables Build
-   Upload bundles

Game:

-   Check catalog
-   Download changed bundles
-   Ready.

------------------------------------------------------------------------

# 10. Typische Fehler vermeiden

-   Tool als Overlay in Gameplay Scene.
-   Gameplay Logic im Tool UI.
-   Alles Addressable machen.
-   Animation Events als Timing verwenden.

------------------------------------------------------------------------

# 11. Architektur Vorteile

-   Stabil
-   Erweiterbar
-   Remote Patch möglich
-   Saubere Trennung Tool vs Game
-   Perfekt für iterative MMO Entwicklung.

------------------------------------------------------------------------

END OF SPEC
