# Phase 5: Ability System

> **Epic:** Fähigkeiten & Action Combat
> **Branch:** `integration/phase-5-ability-system`
> **Abhängigkeit:** Phase 4 (Fortgeschrittene Lokomotion) — ✅ Abgeschlossen
> **Specs:**
> - [Modulare Fertigkeiten Controller v2](../../specs/ModularFertigkeitenController_Spezifikation_v2.md)
> - [Skills & Action Combat](../../specs/GameKit_Skills_ActionCombat.md)
> - [AAA Action Combat & Character Architecture](../../specs/AAA_Action_Combat_Character_Architecture.md)

---

## Ziel

Modulares Ability-Framework als **Infrastruktur** für alle zukünftigen Fähigkeiten (Combat, Utility, Interaction). Phase 5 baut nur das Framework — konkrete Abilities (Attack, Dodge, Cast) folgen in Phase 9.

**Architektur-Entscheidung:** Jump und Sprint bleiben Movement States in der State Machine. Das Ability System ist eine **orthogonale Schicht** für Actions (Angriff, Ausweichen, Zaubern, Interaktion), die parallel zur Bewegung laufen können.

```
Bestehend (Movement Layer):          Neu (Ability Layer):
  Input → State Machine               Input → AbilitySystem
    → Idle, Walk, Run, Sprint            → Attack, Dodge, Cast, Interact
    → Jump, Fall, Land                   → Cooldowns, Priority, Resources
    → CharacterLocomotion                → Animation Layer 1 (Upper Body)
    → CharacterMotor                     → Ability Events
```

---

## Voraussetzungen

- Phase 4 vollständig abgeschlossen (State Machine, Locomotion, Animation CrossFade)
- Bestehendes Animation Layer 1 (Upper Body) im Animator Controller (aktuell leer)
- `IAnimationController.SetAbilityLayerWeight()` bereits implementiert

---

## Package-Entscheidung

Neues Package: **`Wiesenwischer.GameKit.Abilities.Core`**

Begründung: Abilities sind ein eigenständiges System, getrennt von der Movement-Logik. Das Core-Package enthält nur Interfaces, Basis-Typen und den Manager. Konkrete Ability-Implementierungen kommen in separate Packages (z.B. `Abilities.Combat` in Phase 9).

**Abhängigkeit:** `Abilities.Core` → `CharacterController.Core` (für Motor, Locomotion, AnimationController Referenzen)

---

## Schritte

| Schritt | Beschreibung | Branch | Commit-Message |
|---------|-------------|--------|----------------|
| [5.1](5.1-package-iability-interface.md) | Package-Struktur & IAbility Interface | `feat/ability-package-core` | `feat(phase-5): 5.1 Ability Package & IAbility Interface` |
| [5.2](5.2-ability-definition-context.md) | AbilityDefinition & AbilityContext | `feat/ability-package-core` | `feat(phase-5): 5.2 AbilityDefinition & AbilityContext` |
| [5.3](5.3-ability-system-manager.md) | AbilitySystem Manager | `feat/ability-system-manager` | `feat(phase-5): 5.3 AbilitySystem Manager` |
| [5.4](5.4-animation-layer-integration.md) | Animation Layer Integration | `feat/ability-system-manager` | `feat(phase-5): 5.4 Animation Layer Integration` |
| [5.5](5.5-integration-tests.md) | PlayerController Integration & Tests | `feat/ability-integration-tests` | `feat(phase-5): 5.5 PlayerController Integration & Tests` |

**Branch-Zuordnung:**
- `feat/ability-package-core` → Schritte 5.1 + 5.2 (Package + Typen)
- `feat/ability-system-manager` → Schritte 5.3 + 5.4 (Manager + Animation)
- `feat/ability-integration-tests` → Schritt 5.5 (Anbindung + Tests)

---

## Erwartetes Ergebnis

Nach Phase 5:
- Neues Package `Wiesenwischer.GameKit.Abilities.Core` mit vollständigem Ability-Framework
- `IAbility` Interface für alle zukünftigen Abilities
- `AbilityDefinition` ScriptableObject für datengetriebene Konfiguration
- `AbilitySystem` Manager mit Lifecycle, Cooldowns und Priority System
- Animation Layer 1 Integration (Ability-Animationen auf Upper Body)
- `PlayerController` verdrahtet mit AbilitySystem
- Unit Tests für Core-Logik
- **Keine konkreten Abilities** — nur Infrastruktur + Mock-Ability für Tests

---

## Abgrenzung

| In Phase 5 | NICHT in Phase 5 |
|-----------|-----------------|
| IAbility Interface | Konkrete Combat Abilities |
| AbilitySystem Manager | Targeting System |
| Priority System | Damage/Hit System |
| Cooldown-Tracking | Resource/Mana System |
| Animation Layer Integration | AbilityBar UI |
| AbilityDefinition SO | Netzwerk-Sync |
| Unit Tests | Combo/Chain System |

---

## Nächste Phase im Epic

→ Phase 9: Combat Abilities (Melee, Ranged, Spell — nutzt das in Phase 5 erstellte Framework)
