# Phase 31: Adaptive Reconciliation & Smooth Correction

> **Status:** Abgeschlossen
> **Branch:** `integration/phase-31-adaptive-reconciliation`
> **Abhaengigkeit:** Phase 30 (FishNet Native Prediction Migration)

---

## Ziel

Hard-Snap bei Server-Korrekturen durch Smooth Blending ersetzen. Error-Threshold-basierte Strategie: kleine Abweichungen werden ueber mehrere Frames visuell geglaettet, grosse Abweichungen sofort korrigiert (Snap). Ohne diesen Schritt ist jede Reconciliation als Teleport sichtbar — fuer ein MMO inakzeptabel.

## Architektur

**FishNet's NetworkTickSmoother** handhabt BEIDES: Tick-Interpolation UND Reconcile-Correction. Kein custom Smoother noetig.

```
FishNet Tick-Flow:
  OnPreTick (Smoother)       -> Speichert visuelle Position
  OnTick (Driver)            -> SetPositionAndRotation(SimPos) → Simulate
  OnPostTick (Smoother)      -> Queued neue SimPos, stellt Visual zurueck
  OnPostReplicateReplay      -> Eased Reconcile-Corrections ueber Replay-Buffer
  OnUpdate (Smoother)        -> MoveTowards zum naechsten Queue-Target (smooth)
```

**Warum FishNet's Smoother statt custom:**
- Velocity-basiertes MoveTowards (statt Linear Lerp) → gleichmaessige Geschwindigkeit
- Reconcile-Easing ueber den gesamten Replay-Buffer (Power-Kurve) → kein Offset-Akkumulation
- Adaptive Interpolation basierend auf RTT → automatische Latenz-Anpassung
- Buffer-Management mit Speed-Anpassung → keine Queue-Ueberlaeufe
- Teleport-Detection → konfigurierbare Snap-Schwelle

**Verworfene Ansaetze:** Eigener ReconcileSmoother (Goal-Queue + Offset-Decay, Velocity-Based, SmoothDamp, Dead Reckoning, Target-Tracking) — alle fuehrten zu sichtbarem Stutter (5-13:1 Ratio) und/oder Offset-Akkumulation (1.5-3.5m).

**KRITISCH — Zwei-Transform-Architektur:**
```
Player (Root)         ← TargetTransform (Motor, NetworkObject, NetworkCharacterDriver)
  └── Arissa          ← GraphicalTransform (Animator, Mesh, NetworkTickSmoother)
```
FishNet setzt `GraphicalTransform = this.transform`. TargetTransform != GraphicalTransform ist PFLICHT.
NetworkTickSmoother gehoert auf das Visual-Child, TargetTransform zeigt auf den Root.
`DetachOnStart = true` trennt das Visual vom Root, damit es nicht mit-teleportiert.

**KRITISCH — Motor-Sync:** `_motor.Transform.SetPositionAndRotation(TransientPosition)` wird VOR jeder Simulation aufgerufen. FishNet's Smoother schreibt die visuelle Position auf das Visual-Child — der Motor auf dem Root ist davon nicht betroffen.

## Relevante Spezifikationen

- [Phase 30 Spezifikation](../../specs/networking/Wiesenwischer_Phase30_FishNet_Native_Prediction_Migration.md) — Anmerkung "Adaptive Reconciliation"
- [Simulation vs Rendering](../../specs/networking/Wiesenwischer_AAA_Simulation_vs_Rendering.md)

## Schritte

- [x] [31.1 ReconcileSmoother → NetworkTickSmoother Migration](31.1-reconcile-smoother.md)
- [x] [31.2 Owner Reconcile Smoothing](31.2-owner-reconcile-smoothing.md)
- [x] [31.3 Spectator Prediction Verbesserung](31.3-spectator-prediction.md)
- [x] [31.4 Debug Visualization](31.4-debug-visualization.md)
- [x] [31.5 Tests + Dokumentation](31.5-tests-documentation.md)

## Kritische Dateien

| Datei | Aktion |
|-------|--------|
| `Packages/.../Network.FishNet/Runtime/Core/NetworkCharacterDriver.cs` | VEREINFACHT — alle custom Smoother-Logik entfernt |
| `Packages/.../Network.FishNet/Editor/NetworkSetupWizard.cs` | AENDERN — NetworkTickSmoother statt ReconcileSmoother |
| `Packages/.../Network.FishNet/Runtime/Core/ReconcileSmoother.cs` | GELOESCHT |
| `Packages/.../Network.FishNet/Tests/Runtime/Core/ReconcileSmootherTests.cs` | GELOESCHT |
