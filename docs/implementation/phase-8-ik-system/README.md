# Phase 8: IK System

> **Epic:** Natürliche Bewegung — Inverse Kinematics
> **Branch:** `integration/phase-8-ik-system`
> **Abhängigkeit:** Phase 20 (Visual Grounding Smoother) — ✅ In Arbeit (fast abgeschlossen)
> **Specs:**
> - [GameKit IK Spezifikation](../../specs/GameKit_IK_Spezifikation.md)
> - [Animationskonzept LayeredAbilities](../../specs/Animationskonzept_LayeredAbilities.md)
> - [AAA Action Combat & Character Architecture](../../specs/AAA_Action_Combat_Character_Architecture.md)

---

## Ziel

Modulares IK-System für natürliche Character-Bewegung: Füße passen sich dem Terrain an (Foot IK), Kopf/Körper blicken dem Kamera-Ziel nach (LookAt IK). Das System ist modular erweiterbar — Hand IK für Waffen/Interaktionen folgt nach dem Ability System (Phase 9+).

**Kern-Prinzip:** IK ist eine rein visuelle Schicht, die NACH der Motor-Physik arbeitet. Der `IKManager` orchestriert unabhängige `IIKModule`-Implementierungen und ruft sie im `OnAnimatorIK`-Callback auf. Module können einzeln aktiviert/deaktiviert werden.

```
Bestehend (Motor/Animation):                Neu (IK Layer):
  CharacterMotor → TransientPosition          IKManager (OnAnimatorIK)
  CharacterLocomotion → GroundInfo               ├── FootIK (Terrain Adaptation)
  GroundingSmoother → Visual Y-Smoothing         └── LookAtIK (Head Tracking)
  AnimatorParameterBridge → Blend Parameters     IIKTargetProvider (Kamera, etc.)
```

---

## Voraussetzungen

- Phase 20 abgeschlossen (GroundingSmoother, visuelles Step-Smoothing)
- Phase 4 abgeschlossen (Locomotion, GroundInfo, Detection Strategies)
- CharacterMotor mit `GroundingStatus`, `TransientPosition`
- Animator Layer 0 hat `iKOnFeet = true` (bereits konfiguriert)
- Player Prefab mit CharacterModel als Child-Object (Animator darauf)

---

## Package-Entscheidung

Neues Package: **`Wiesenwischer.GameKit.CharacterController.IK`**

Begründung: IK ist ein optionaler visueller Layer. Ohne Package kein IK — kein Einfluss auf Gameplay-Logik. Trennung von Animation (Blend Trees, CrossFade) und IK (Runtime-Positionsanpassung) ist sauber und erlaubt IK bei Bedarf abzuschalten.

**Pfad:** `Packages/Wiesenwischer.GameKit.CharacterController.IK/`

**Abhängigkeiten:**
- `Wiesenwischer.GameKit.CharacterController.Core` (Motor, Locomotion, GroundInfo)
- `Wiesenwischer.GameKit.CharacterController.Animation` (AnimationParameters für Layer-Indices)

---

## Schritte

| Schritt | Beschreibung | Branch | Commit-Message |
|---------|-------------|--------|----------------|
| [8.1](8.1-package-interfaces.md) | Package-Struktur & IIKModule Interface | `feat/ik-package-interfaces` | `feat(phase-8): 8.1 IK Package & IIKModule Interface` |
| [8.2](8.2-ik-manager.md) | IKManager Komponente | `feat/ik-package-interfaces` | `feat(phase-8): 8.2 IKManager Komponente` |
| [8.3](8.3-foot-ik.md) | FootIK Modul | `feat/ik-foot-lookat` | `feat(phase-8): 8.3 FootIK Modul` |
| [8.4](8.4-lookat-ik.md) | LookAtIK Modul | `feat/ik-foot-lookat` | `feat(phase-8): 8.4 LookAtIK Modul` |
| [8.5](8.5-integration-tests.md) | Prefab-Integration & Tests | `feat/ik-integration-tests` | `feat(phase-8): 8.5 Prefab-Integration & Tests` |

**Branch-Zuordnung:**
- `feat/ik-package-interfaces` → Schritte 8.1 + 8.2 (Package + Manager)
- `feat/ik-foot-lookat` → Schritte 8.3 + 8.4 (Foot + LookAt Module)
- `feat/ik-integration-tests` → Schritt 8.5 (Prefab + Tests)

---

## Erwartetes Ergebnis

Nach Phase 8:
- Neues Package `Wiesenwischer.GameKit.CharacterController.IK`
- `IIKModule` Interface für erweiterbare IK-Module
- `IKManager` orchestriert Module im `OnAnimatorIK`-Callback
- `FootIK` Modul — Füße passen sich dem Terrain an (Raycasts, Body Offset)
- `LookAtIK` Modul — Kopf/Körper tracken das Kamera-Ziel
- Player Prefab enthält IKManager mit konfigurierten Modulen
- Unit Tests für IK-Berechnungen
- Visuell: Füße stehen korrekt auf Stufen/Slopes, Kopf dreht sich zur Kamera

---

## Abgrenzung

| In Phase 8 | NICHT in Phase 8 |
|-----------|-----------------|
| IIKModule Interface | Hand IK (→ Phase 9+) |
| IKManager Komponente | Netzwerk-Sync (→ Phase 7) |
| FootIK (Terrain-Anpassung) | Climbing/Ledge IK |
| LookAtIK (Head Tracking) | IK für Reiten/Gleiten (→ Phase 10) |
| Unit Tests | Runtime-Konfiguration per ScriptableObject |
| Prefab-Integration | IK-Blending mit Abilities |

---

## Nächste Phase im Epic

→ (Kein direkter Nachfolger im IK Epic. Hand IK wird nach Phase 9: Combat Abilities ergänzt.)
