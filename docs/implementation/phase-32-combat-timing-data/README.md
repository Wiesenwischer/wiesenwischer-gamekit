# Phase 32: Combat Timing Data Model & Runtime

> **Epic:** [Combat Animation Tuning Tool](../README.md#combat-animation-tuning-tool)
> **Branch:** `integration/phase-32-combat-timing-data`
> **Status:** Offen
> **Abhängigkeiten:** Phase 5 (Ability System) — AbilitySystem als bestehendes Framework

---

## Ziel

Data-driven Combat Timing Fundament aufbauen. AttackDefinition mit Frame-basiertem Timing, Attack Mapping Layer zur Entkopplung von Animation und Gameplay-Daten, Runtime Frame Berechnung und Hit Detection.

Dieses Fundament dient als Grundlage für:
- **Phase 33/34:** Combat Animation Tuning Tool (Runtime Editor)
- **Phase 9:** Combat Abilities (MeleeAbility, RangedAbility etc. konsumieren AttackDefinition)

**Kernprinzipien (aus Spec):**
- Animation Events werden **NICHT** für Gameplay Timing verwendet
- Animation = reiner Visual Layer, Gameplay Timing = Data-driven
- Timing in **Frames** (nicht Sekunden) — deterministisch, stabil bei Speed-Änderungen
- Gameplay Anchor = `attackStartTime` im Code (netzwerkfreundlich)

---

## Relevante Spezifikationen

| Dokument | Relevanz |
|----------|----------|
| [Combat Animation Tuning Tool Specification](../../specs/animation-tooling/CombatAnimationTuningTool_Specification.md) | Haupt-Spec — Kapitel 2–5, 12 |
| [AAA Action Combat & Character Architecture](../../specs/AAA_Action_Combat_Character_Architecture.md) | Combat-Architektur-Kontext |
| [Skills & Action Combat](../../specs/GameKit_Skills_ActionCombat.md) | Ability-Integration-Kontext |

---

## Architektur-Entscheidungen

### Neues Package: `Wiesenwischer.GameKit.Combat.Core` (unabhängig von Abilities.Core)

**Entscheidung:** `Combat.Core` hat **keine Abhängigkeit** zu `Abilities.Core` oder anderen GameKit-Packages.

**Geprüfte Alternativen:**

| Option | Beschreibung | Bewertung |
|--------|-------------|-----------|
| **A: Unabhängig (gewählt)** | Combat.Core standalone, Integration in Phase 9 über eine `CombatAbility`-Klasse die beide Packages referenziert | Sauberste Trennung, Tuning Tool bleibt schlank |
| **B: Combat.Core → Abilities.Core** | Combat.Core referenziert Abilities.Core, `AttackDefinition` kennt `AbilityDefinition` | Weniger Verdrahtung in Phase 9, aber Tuning Tool zieht transitiv Abilities.Core + CharacterController.Core rein |
| **C: AbilityDefinition erweitern** | Frame-Timing-Felder direkt auf `AbilityDefinition` (ScriptableObject) | Am einfachsten, aber: SO ist im Build nicht runtime-editierbar → bricht Tuning Tool. Und: polluted non-Combat Abilities mit Combat-Feldern |

**Warum Option A gewählt wurde:**

1. **Persistenz-Inkompatibilität:** `AbilityDefinition` ist ein **ScriptableObject** (Editor-Daten, im Build nicht editierbar). `AttackDefinition` muss **`[Serializable]`** sein für Runtime-JSON-Editing im Tuning Tool. Diese Anforderungen schließen sich gegenseitig aus.
2. **Tuning Tool Scope:** Phase 33/34 braucht nur Combat-Timing-Daten + Animator — kein AbilitySystem-Lifecycle (Cooldown, Priority, Activation). Abhängigkeit zu Abilities.Core würde transitiv `CharacterController.Core` reinziehen.
3. **Nicht jede Ability ist Combat:** Buffs, Heals, Utility-Abilities brauchen kein Frame-Timing. Erweiterung von AbilityDefinition würde diese unnötig aufblähen.
4. **Kosten der Trennung sind gering:** Phase 9 braucht lediglich eine `CombatAbility`-Klasse die `IAbility` implementiert und eine `AttackDefinition`-Referenz hält — kein eigenes Bridge-Package nötig.
5. **Richtung der Erweiterbarkeit:** Abhängigkeit hinzufügen ist einfach (ein Eintrag in .asmdef), entfernen ist schwer. Falls sich in Phase 9 herausstellt, dass die Trennung unpraktisch ist, kann die Referenz jederzeit nachgezogen werden.

### Package-Abhängigkeiten

```
Wiesenwischer.GameKit.Combat.Core
  └── (keine GameKit-Abhängigkeiten — standalone Datenmodell)

Spätere Integration (Phase 9):
  CombatAbility-Klasse (in Abilities.Core oder Game-Assembly)
    referenziert: Wiesenwischer.GameKit.Combat.Core (AttackDefinition)
    referenziert: Wiesenwischer.GameKit.Abilities.Core (IAbility, AbilityDefinition)
```

### Datenmodell: Serializable statt ScriptableObject

`AttackDefinition` ist `[Serializable]` (keine ScriptableObject), weil:
- Runtime Tool muss Daten zur Laufzeit editieren und speichern (JSON)
- ScriptableObjects sind Editor-Daten und nicht runtime-editierbar im Build
- `AttackDatabase` (ScriptableObject) hält eine Collection von AttackDefinitions für Editor-Workflow

---

## Package-Struktur

```
Packages/Wiesenwischer.GameKit.Combat.Core/
├── package.json
├── Runtime/
│   ├── Wiesenwischer.GameKit.Combat.Core.Runtime.asmdef
│   ├── Data/
│   │   ├── AttackDefinition.cs       (Frame-Timing, Damage, Serializable)
│   │   ├── HitWindow.cs              (Start/End Frame, Serializable)
│   │   ├── AttackDatabase.cs          (ScriptableObject, Collection)
│   │   ├── AttackMapping.cs           (Animation ↔ Attack Entkopplung)
│   │   └── AttackMappingDatabase.cs   (ScriptableObject, Mapping-Collection)
│   └── Core/
│       ├── FrameCalculator.cs         (Statische Frame-Berechnung)
│       ├── AttackRuntime.cs           (Laufzeit-Tracking, MonoBehaviour)
│       ├── IHitboxController.cs       (Interface für Hitbox Enable/Disable)
│       └── AttackPersistence.cs       (JSON Import/Export)
└── Tests/
    └── Runtime/
        ├── Wiesenwischer.GameKit.Combat.Core.Tests.Runtime.asmdef
        └── Core/
            ├── AttackDefinitionTests.cs
            ├── FrameCalculatorTests.cs
            ├── HitDetectionTests.cs
            └── AttackPersistenceTests.cs
```

---

## Schritte

| Schritt | Name | Branch | Commit-Message |
|---------|------|--------|----------------|
| [32.1](32.1-package-grundtypen.md) | Package-Struktur & Grundtypen | `feat/combat-package-structure` | `feat(phase-32): 32.1 Combat.Core Package-Struktur & Grundtypen` |
| [32.2](32.2-attack-definition-database.md) | AttackDefinition & AttackDatabase | `feat/attack-definition-database` | `feat(phase-32): 32.2 AttackDefinition & AttackDatabase` |
| [32.3](32.3-attack-mapping.md) | AttackMapping Layer | `feat/attack-mapping` | `feat(phase-32): 32.3 AttackMapping Layer` |
| [32.4](32.4-attack-runtime.md) | AttackRuntime — Frame Berechnung | `feat/attack-runtime` | `feat(phase-32): 32.4 AttackRuntime Frame Berechnung` |
| [32.5](32.5-hit-detection.md) | Hit Detection System | `feat/hit-detection` | `feat(phase-32): 32.5 Hit Detection System` |
| [32.6](32.6-json-persistenz.md) | JSON Serialisierung / Persistenz | `feat/combat-json-persistenz` | `feat(phase-32): 32.6 JSON Serialisierung & Persistenz` |
| [32.7](32.7-unit-tests.md) | Unit Tests | `feat/combat-unit-tests` | `feat(phase-32): 32.7 Unit Tests` |

---

## Voraussetzungen

- Phase 5 (Ability System) abgeschlossen ✅
- Konsolidierte Spec gelesen: `docs/specs/animation-tooling/CombatAnimationTuningTool_Specification.md`

---

## Erwartetes Ergebnis

Nach Abschluss dieser Phase:

1. **Package `Wiesenwischer.GameKit.Combat.Core`** kompiliert fehlerfrei
2. **AttackDefinition** mit Frame-basiertem Timing (HitWindow, totalFrames, damage)
3. **AttackMapping** entkoppelt Animation von Gameplay-Daten
4. **FrameCalculator** berechnet deterministisch currentFrame aus attackStartTime
5. **AttackRuntime** trackt laufende Attacks und detektiert Hit Windows
6. **IHitboxController** ermöglicht Hitbox Enable/Disable durch Frame-Timing
7. **AttackPersistence** liest/schreibt AttackDefinitions als JSON (Runtime-fähig)
8. **Unit Tests** decken alle Kernfunktionen ab
9. **Keine Abhängigkeiten** zu Abilities.Core oder anderen GameKit-Packages

---

## Nächste Phase

→ [Phase 33: Combat Preview Scene & Runtime Editor](../phase-33-combat-preview-editor/README.md)
