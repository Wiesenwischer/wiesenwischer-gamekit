# Phase 25: Foot Locking (Anti-Sliding)

> **Epic:** Natürliche Bewegung — Inverse Kinematics
> **Branch:** `integration/phase-25-foot-locking`
> **Abhängigkeiten:** Phase 8 (IK System), Phase 24 (FootIK Verbesserungen)
> **Status:** Offen

---

## Ziel

Foot Sliding eliminieren bei Animations-Übergängen (Walk→Idle, Run→Stop). Füße werden per Velocity-Erkennung automatisch an ihrer Welt-Position "festgenagelt" solange sie am Boden stehen. Kein Animations-Kurven-Authoring nötig — funktioniert mit allen Mixamo-Animationen.

## Spezifikation

- [FootLock Spezifikation](../../specs/FootLock_Spezifikation.md)
- [GameKit IK Spezifikation](../../specs/GameKit_IK_Spezifikation.md)

## Voraussetzungen

- IK-Package mit IKManager, IIKModule, FootIK (Phase 8 ✅)
- FootIK Terrain-Adaptive Verbesserungen (Phase 24 ✅)
- Player Prefab mit IK-Komponenten

## Architektur

```
IKManager (orchestriert)
├── FootLock (Anti-Sliding)       — NEU, registriert sich ZUERST
├── FootIK   (Terrain-Anpassung)  — existierend, prüft Lock-Flags
└── LookAtIK (Kamera-Blick)      — existierend, nicht betroffen
```

**Koordination:** FootLock setzt `IsLeftFootLocked`/`IsRightFootLocked` Flags. FootIK prüft diese und überspringt gelockte Füße.

## Schritte

| # | Schritt | Commit-Message | Branch |
|---|---------|---------------|--------|
| 25.1 | [FootLock Modul implementieren](25.1-footlock-module.md) | `feat(phase-25): 25.1 FootLock Modul mit Velocity-Erkennung` | `feat/footlock-module` |
| 25.2 | [FootIK Koordination](25.2-footik-coordination.md) | `feat(phase-25): 25.2 FootIK prüft FootLock-Flags` | `feat/footlock-module` |
| 25.3 | [IKSetupWizard Erweiterung](25.3-wizard-extension.md) | `feat(phase-25): 25.3 IKSetupWizard um FootLock erweitern` | `feat/footlock-wizard` |
| 25.4 | [Unit Tests](25.4-unit-tests.md) | `test(phase-25): 25.4 FootLock Unit Tests` | `test/footlock-tests` |
| 25.5 | [Play Mode Verifikation](25.5-play-mode-verifikation.md) | `docs(phase-25): 25.5 Play Mode Verifikation` | — |

## Erwartetes Ergebnis

Nach Abschluss:
- Neues `FootLock.cs` Modul im IK-Package
- FootIK respektiert Lock-Flags und überspringt gelockte Füße
- IKSetupWizard fügt FootLock automatisch hinzu
- Kein sichtbares Foot Sliding bei Walk→Idle / Run→Stop Übergängen
- Unit Tests für Velocity-Erkennung, Lock/Release, Local-Space

## Design-Hinweise

### Registrierungsreihenfolge
FootLock muss sich VOR FootIK beim IKManager registrieren. Dies wird durch die Component-Reihenfolge auf dem GameObject sichergestellt (FootLock vor FootIK hinzufügen).

### IKManager Airborne-Deaktivierung
Wenn `_disableDuringAirborne = true`, werden PrepareIK/ProcessIK aller Module während Sprüngen übersprungen. FootLock's Zustand wird eingefroren. Beim Landen ergibt die Velocity-Berechnung mit veralteter prevPos einen Spike → Lock wird automatisch gelöst. Kein expliziter Airborne-Reset nötig.

### Transform-Referenz
FootLock speichert Positionen relativ zum **Player-Root-Transform** (nicht dem Model-Transform), da der GroundingSmoother das Model-Transform Y-versetzt. So bleiben gelockte Positionen stabil.

---

**Nächste Phase im Epic:** Keine geplant (Post-MVP: Foot Replanting, Animation Curves)
