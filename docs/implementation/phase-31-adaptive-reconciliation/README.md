# Phase 31: Adaptive Reconciliation & Smooth Correction

> **Status:** Abgeschlossen
> **Branch:** `integration/phase-31-adaptive-reconciliation`
> **Abhaengigkeit:** Phase 30 (FishNet Native Prediction Migration)

---

## Ziel

Hard-Snap bei Server-Korrekturen durch Smooth Blending ersetzen. Error-Threshold-basierte Strategie: kleine Abweichungen werden ueber mehrere Frames visuell geglaettet, grosse Abweichungen sofort korrigiert (Snap). Ohne diesen Schritt ist jede Reconciliation als Teleport sichtbar — fuer ein MMO inakzeptabel.

## Architektur

**Correction-Offset-Pattern:** Nach Reconcile + Replay wird der Fehler (pre-reconcile Position vs. post-replay Position) als visueller Offset gespeichert und exponentiell abgebaut.

```
Tick-Flow (FishNet OnTick):
  PreSimulationInterpolationUpdate()    -> InitialTickPosition = TransientPosition
  PerformReconcile() [wenn Server-Daten] -> Snap zu Server-Position
  PerformReplicate(Replayed) x N        -> Replay aller Ticks
  PerformReplicate(Ticked)              -> Aktueller Tick (HIER: Error berechnen)
  PostSimulationInterpolationUpdate()   -> Timestamps fuer Frame-Interpolation

LateUpdate (CharacterMotorSystem, ExecOrder -100):
  CustomInterpolationUpdate()           -> Lerp(InitialTickPos, TransientPos, factor)

LateUpdate (ReconcileSmoother, ExecOrder 100):
  DecayAndApplyCorrectionOffset()       -> transform.position += decayingOffset
```

**Entscheidung:** Eigener ReconcileSmoother statt FishNet's NetworkTickSmoother, weil letzterer getrennte GameObjects (SimObj + VisualRoot) erfordert. Unser Motor trennt bereits TransientPosition (Simulation) von Transform.position (Visual) auf dem gleichen GameObject.

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
