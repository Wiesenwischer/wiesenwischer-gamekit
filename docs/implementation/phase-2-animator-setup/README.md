# Phase 2: Animator Setup

**Branch:** `feature/phase-2-animator-setup`
**Abhängigkeiten:** Phase 1 (Animation-Vorbereitung)
**Geschätzte Dauer:** ~3 Stunden

---

## Ziel

Den Unity Animator Controller vollständig konfigurieren:
- Avatar Masks für Ober-/Unterkörper-Trennung (Layer-System)
- Animator Controller mit Layer-Struktur (Base Movement + Abilities)
- Locomotion Blend Tree (Idle → Walk → Run → Sprint)
- Airborne States (Jump, Fall, SoftLand, HardLand)
- Parameter-Bridge Komponente (State Machine → Animator Synchronisierung)

---

## Relevante Spezifikationen

- [Animationskonzept LayeredAbilities](../../specs/Animationskonzept_LayeredAbilities.md)

---

## Schritte

| # | Schritt | Commit-Message | Status |
|---|---------|----------------|--------|
| 2.1 | [Avatar Masks erstellen](2.1-avatar-masks.md) | `feat(phase-2): 2.1 Avatar Masks erstellen` | [ ] |
| 2.2 | [Animator Controller erstellen](2.2-animator-controller.md) | `feat(phase-2): 2.2 Animator Controller erstellen` | [ ] |
| 2.3 | [Locomotion Blend Tree](2.3-locomotion-blend-tree.md) | `feat(phase-2): 2.3 Locomotion Blend Tree` | [ ] |
| 2.4 | [Airborne States](2.4-airborne-states.md) | `feat(phase-2): 2.4 Airborne States` | [ ] |
| 2.5 | [Parameter-Bridge](2.5-parameter-bridge.md) | `feat(phase-2): 2.5 Parameter-Bridge` | [ ] |

---

## Voraussetzungen

- Phase 1 abgeschlossen:
  - Character FBX mit Humanoid Avatar importiert
  - Basis-Animationen vorhanden (Idle, Walk, Run, Sprint, Jump, Fall, Land)
  - Animation Package Struktur erstellt (`Wiesenwischer.GameKit.CharacterController.Animation`)
  - `IAnimationController` Interface und `AnimationParameters` Klasse vorhanden
- Unity Projekt geöffnet

---

## Architektur-Überblick

### Layer-Struktur (aus Spezifikation)

```
Layer 0: Base Movement (Weight 1.0, keine Mask)
├── Locomotion (Blend Tree: Idle → Walk → Run → Sprint)
├── Jump
├── Fall
├── SoftLand
└── HardLand

Layer 1: Abilities (Weight 0.0 default, UpperBody Mask)
├── Empty (Default)
├── Cast_Fireball
├── Attack_Melee
└── ... (wird in Phase 4+ erweitert)
```

### Animator-Parameter

| Parameter | Typ | Beschreibung |
|-----------|-----|-------------|
| `Speed` | Float | Normalisierte Bewegungsgeschwindigkeit (0=Idle, 0.5=Walk, 1.0=Run, 1.5=Sprint) |
| `IsGrounded` | Bool | Ob der Character am Boden ist |
| `VerticalVelocity` | Float | Vertikale Geschwindigkeit (positiv=aufsteigend, negativ=fallend) |
| `Jump` | Trigger | Löst Jump-Animation aus |
| `Land` | Trigger | Löst Land-Animation aus |
| `HardLanding` | Bool | Ob die Landung eine harte Landung ist (für Land-Transition-Auswahl) |

### Speed-Normalisierung

Die Bridge normalisiert die horizontale Geschwindigkeit relativ zu `RunSpeed`:

```
Speed = horizontalVelocity.magnitude / Config.RunSpeed

Ergebnis:
  Idle     → 0.0
  Walk     → WalkSpeed / RunSpeed    = 3/6 = 0.5
  Run      → RunSpeed / RunSpeed     = 6/6 = 1.0
  Sprint   → SprintSpeed / RunSpeed  = 9/6 = 1.5
```

---

## Ergebnis

Nach Abschluss dieser Phase:
- [ ] Avatar Masks erstellt (UpperBody, LowerBody, ArmsOnly)
- [ ] Animator Controller mit 2 Layern (Base Movement, Abilities)
- [ ] Locomotion Blend Tree mit 4 Clips (Idle, Walk, Run, Sprint)
- [ ] Airborne States mit korrekten Transitionen
- [ ] Parameter-Bridge Komponente synchronisiert State Machine mit Animator
- [ ] Alle Dateien kompilieren fehlerfrei

---

## Nächste Phase

[Phase 3: Animation-Integration](../phase-3-animation-integration/README.md)
