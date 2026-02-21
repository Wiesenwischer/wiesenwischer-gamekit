# Phase 31: Adaptive Reconciliation & Smooth Correction

> **Status:** Abgeschlossen
> **Branch:** `integration/phase-31-adaptive-reconciliation`
> **Abhaengigkeit:** Phase 30 (FishNet Native Prediction Migration)

---

## Ziel

Hard-Snap bei Server-Korrekturen durch Smooth Blending ersetzen. Error-Threshold-basierte Strategie: kleine Abweichungen werden ueber mehrere Frames visuell geglaettet, grosse Abweichungen sofort korrigiert (Snap). Ohne diesen Schritt ist jede Reconciliation als Teleport sichtbar — fuer ein MMO inakzeptabel.

## Architektur

**Goal-Queue Interpolation + Correction-Offset:** ReconcileSmoother ist das einzige System das `Transform.position` in LateUpdate schreibt. KCC-Interpolation (`CustomInterpolationUpdate`) ist deaktiviert (`CharacterMotorSystem.Settings.Interpolate = false`).

```
Tick-Flow (FishNet OnTick/OnPostTick):
  OnPreTick()                           -> Initialisierung (einmalig beim ersten Tick)
  PerformReconcile() [wenn Server-Daten] -> Snap zu Server-Position
  PerformReplicate(Replayed) x N        -> Replay aller Ticks
  PerformReplicate(Ticked)              -> Aktueller Tick (HIER: OnReconcileComplete)
  OnPostTick()                          -> Motor-Position in Goal-Queue pushen

LateUpdate (ReconcileSmoother, ExecOrder 50):
  Goal-Queue konsumieren                -> Lerp(_fromPos, _toPos, interpT)
  Offset-Decay                          -> Exponentieller Abbau des Reconcile-Offsets
  Final Visual                          -> transform.position = interpPos + offset
```

**Goal-Queue Prinzip:**
- Jeder Tick pusht die Motor-Position als Goal in eine FIFO-Queue
- LateUpdate konsumiert Goals mit konstanter Rate (1 pro tickDelta)
- Visual interpoliert per Lerp zwischen aufeinanderfolgenden Goals
- Multi-Tick-Frames: Queue puffert, LateUpdate konsumiert normal
- Tick-Luecken: Visual haelt bei letztem Goal (kein Overshoot)
- Adaptive Catchup: Bei Queue-Wachstum leicht schnellere Consumption statt Hard-Drop

**Correction-Offset:** Nach Reconcile + Replay wird der Fehler (pre-reconcile vs. post-replay) als visueller Offset gespeichert und exponentiell abgebaut. Gleichzeitig werden alle Interpolations-Punkte (from, to, Queue) um die Korrektur verschoben → Visual bleibt stabil, Offset decayed zur korrigierten Trajektorie.

**Entscheidung:** Eigener ReconcileSmoother statt FishNet's NetworkTickSmoother, weil letzterer getrennte GameObjects (SimObj + VisualRoot) erfordert. Unser Motor trennt bereits TransientPosition (Simulation) von Transform.position (Visual) auf dem gleichen GameObject.

**Verworfene Ansaetze:** Velocity-Based Movement (Extrapolation → Drift-Akkumulation bei unregelmaessigem Tick-Timing), Target-Tracking (MoveTowards/Exponential Smoothing → variable visuelle Geschwindigkeit), SmoothDamp, Dead Reckoning — alle fuehrten zu sichtbarem Stutter (5-13:1 Ratio).

## Relevante Spezifikationen

- [Phase 30 Spezifikation](../../specs/networking/Wiesenwischer_Phase30_FishNet_Native_Prediction_Migration.md) — Anmerkung "Adaptive Reconciliation"
- [Simulation vs Rendering](../../specs/networking/Wiesenwischer_AAA_Simulation_vs_Rendering.md)

## Schritte

- [x] [31.1 ReconcileSmoother Komponente](31.1-reconcile-smoother.md)
- [x] [31.2 Owner Reconcile Smoothing](31.2-owner-reconcile-smoothing.md)
- [x] [31.3 Spectator Prediction Verbesserung](31.3-spectator-prediction.md)
- [x] [31.4 Debug Visualization](31.4-debug-visualization.md)
- [x] [31.5 Tests + Dokumentation](31.5-tests-documentation.md)

## Kritische Dateien

| Datei | Aktion |
|-------|--------|
| `Packages/.../Network.FishNet/Runtime/Core/ReconcileSmoother.cs` | NEU |
| `Packages/.../Network.FishNet/Runtime/Core/NetworkCharacterDriver.cs` | AENDERN |
| `Packages/.../Network.FishNet/Tests/Runtime/Core/ReconcileSmootherTests.cs` | NEU |
