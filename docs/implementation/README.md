# Implementierungsplan - GameKit Character Controller

> **Letzte Aktualisierung:** 2026-02-07
> **Aktuelle Phase:** Phase 1 - Animation-Vorbereitung
> **Aktueller Schritt:** 1.1

---

## Übersicht

Dieser Plan beschreibt die schrittweise Implementierung des GameKit Character Controllers mit Animation, Abilities und Netzwerk-Unterstützung.

**Prinzipien:**
- Jede Phase = eigener Feature-Branch
- Jeder Schritt = eigener Commit
- Dokumentation vor Implementierung
- Spezifikationen **müssen** vor Implementierung gelesen werden

---

## Phasen-Übersicht

| Phase | Name | Ausgearbeitet | Status |
|-------|------|---------------|--------|
| 1 | Animation-Vorbereitung | ✅ | Offen |
| 2 | Animator Setup | ✅ | Offen |
| 3 | Animation-Integration | ❌ | Offen |
| 4 | Ability System | ❌ | Offen |
| 5 | Netzwerk-Grundstruktur | ❌ | Offen |
| 6 | Netzwerk-Animation | ❌ | Offen |
| 7 | IK System | ❌ | Offen |
| 8 | Combat Abilities | ❌ | Offen |
| 9 | Alternative Movement | ❌ | Offen |

---

## Tracking

### Phase 1: Animation-Vorbereitung
**Branch:** `feature/phase-1-animation-prep`
**Ausgearbeitet:** ✅ Ja

**Relevante Spezifikationen:**
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)
- [GameKit CharacterController Modular](../specs/GameKit_CharacterController_Modular.md)

**Schritte:**
- [ ] [1.1 Character Asset beschaffen](phase-1-animation-prep/1.1-character-asset.md)
- [ ] [1.2 Character in Unity importieren](phase-1-animation-prep/1.2-import-character.md)
- [ ] [1.3 Basis-Animationen beschaffen](phase-1-animation-prep/1.3-animations.md)
- [ ] [1.4 Animation Package Struktur](phase-1-animation-prep/1.4-package-structure.md)

---

### Phase 2: Animator Setup
**Branch:** `feature/phase-2-animator-setup`
**Ausgearbeitet:** ✅ Ja

**Relevante Spezifikationen:**
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)

**Schritte:**
- [ ] [2.1 Avatar Masks erstellen](phase-2-animator-setup/2.1-avatar-masks.md)
- [ ] [2.2 Animator Controller erstellen](phase-2-animator-setup/2.2-animator-controller.md)
- [ ] [2.3 Locomotion Blend Tree](phase-2-animator-setup/2.3-locomotion-blend-tree.md)
- [ ] [2.4 Airborne States](phase-2-animator-setup/2.4-airborne-states.md)
- [ ] [2.5 Parameter-Bridge](phase-2-animator-setup/2.5-parameter-bridge.md)

---

### Phase 3: Animation-Integration
**Branch:** `feature/phase-3-animation-integration`
**Ausgearbeitet:** ❌ Nein

**Relevante Spezifikationen:**
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)
- [GameKit IK Spezifikation](../specs/GameKit_IK_Spezifikation.md)

**Schritte:**
- [ ] 3.1 IAnimationController Interface
- [ ] 3.2 CharacterAnimator Komponente
- [ ] 3.3 State Machine Sync
- [ ] 3.4 Player Prefab
- [ ] 3.5 Testing & Feintuning

---

### Phase 4: Ability System
**Branch:** `feature/phase-4-ability-system`
**Ausgearbeitet:** ❌ Nein

**Relevante Spezifikationen:**
- [Modulare Fertigkeiten Controller v2](../specs/ModularFertigkeitenController_Spezifikation_v2.md)
- [Skills & Action Combat](../specs/GameKit_Skills_ActionCombat.md)

**Schritte:**
- [ ] 4.1 IAbility Interface
- [ ] 4.2 AbilitySystem Manager
- [ ] 4.3 JumpAbility
- [ ] 4.4 SprintAbility
- [ ] 4.5 Animation Layer Integration

---

### Phase 5: Netzwerk-Grundstruktur
**Branch:** `feature/phase-5-network`
**Ausgearbeitet:** ❌ Nein

**Relevante Spezifikationen:**
- [CSP Spezifikation](../specs/CSP_Spezifikation.md)
- [GameKit MMO Basics](../specs/GameKit_MMO_Basics.md)
- [GameKit InputSystem Spezifikation](../specs/GameKit_InputSystem_Spezifikation.md)

**Schritte:**
- [ ] 5.1 FishNet einbinden
- [ ] 5.2 NetworkPlayer
- [ ] 5.3 Input Sync
- [ ] 5.4 Position/Rotation Sync
- [ ] 5.5 Client-Side Prediction

---

### Phase 6: Netzwerk-Animation
**Branch:** `feature/phase-6-network-animation`
**Ausgearbeitet:** ❌ Nein

**Relevante Spezifikationen:**
- [CSP Spezifikation](../specs/CSP_Spezifikation.md)
- [GameKit MMO Basics](../specs/GameKit_MMO_Basics.md)

**Schritte:**
- [ ] 6.1 Animator Sync
- [ ] 6.2 State Sync
- [ ] 6.3 Ability Sync
- [ ] 6.4 Lag Compensation

---

### Phase 7: IK System
**Branch:** `feature/phase-7-ik-system`
**Ausgearbeitet:** ❌ Nein

**Relevante Spezifikationen:**
- [GameKit IK Spezifikation](../specs/GameKit_IK_Spezifikation.md)
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)

**Schritte:**
- [ ] 7.1 IK Package Struktur
- [ ] 7.2 IKManager Komponente
- [ ] 7.3 LookAtIK Implementation
- [ ] 7.4 FootIK Implementation
- [ ] 7.5 HandIK Implementation
- [ ] 7.6 IK Netzwerk-Sync

---

### Phase 8: Combat Abilities
**Branch:** `feature/phase-8-combat-abilities`
**Ausgearbeitet:** ❌ Nein

**Relevante Spezifikationen:**
- [Skills & Action Combat](../specs/GameKit_Skills_ActionCombat.md)
- [Modulare Fertigkeiten Controller v2](../specs/ModularFertigkeitenController_Spezifikation_v2.md)
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)

**Schritte:**
- [ ] 8.1 Combat Package Struktur
- [ ] 8.2 MeleeAbility (Nahkampf)
- [ ] 8.3 RangedAbility (Fernkampf/Bogen)
- [ ] 8.4 SpellAbility (Zauber)
- [ ] 8.5 Combat Animationen
- [ ] 8.6 Combat Netzwerk-Sync

---

### Phase 9: Alternative Movement
**Branch:** `feature/phase-9-alternative-movement`
**Ausgearbeitet:** ❌ Nein

**Relevante Spezifikationen:**
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)
- [GameKit CharacterController Modular](../specs/GameKit_CharacterController_Modular.md)

**Schritte:**
- [ ] 9.1 Movement Controller Abstraktion
- [ ] 9.2 RidingController (Reiten)
- [ ] 9.3 GlidingController (Gleiten)
- [ ] 9.4 SwimmingController (Schwimmen)
- [ ] 9.5 Movement Animationen
- [ ] 9.6 Movement Netzwerk-Sync

---

## Abhängigkeiten

```
Phase 1 ──> Phase 2 ──> Phase 3 ──> Phase 7 (IK)
                            │
                            v
                       Phase 4 ──> Phase 8 (Combat)
                            │
                            v
                       Phase 5 ──> Phase 6
                            │
                            v
                       Phase 9 (Alt. Movement)
```

**Erläuterung:**
- Phase 7 (IK) baut auf Animation-Integration (Phase 3) auf
- Phase 8 (Combat) baut auf Ability System (Phase 4) auf
- Phase 9 (Alt. Movement) kann nach Netzwerk-Grundstruktur erfolgen

---

## Workflow

### Neue Phase starten
```bash
git checkout main
git pull origin main
git checkout -b feature/phase-X-name
```

### Schritt abschließen
```bash
git add .
git commit -m "feat(phase-X): X.Y Beschreibung"
```

### Phase abschließen
```bash
git push -u origin feature/phase-X-name
gh pr create --title "feat: Phase X - Name"
# Nach Review: Merge
```

---

## Zeitschätzung

| Phase | Beschreibung | Dauer |
|-------|--------------|-------|
| 1 | Animation-Vorbereitung | ~2h |
| 2 | Animator Setup | ~3h |
| 3 | Animation-Integration | ~3h |
| 4 | Ability System | ~4h |
| 5 | Netzwerk-Grundstruktur | ~5h |
| 6 | Netzwerk-Animation | ~4h |
| 7 | IK System | ~4h |
| 8 | Combat Abilities | ~6h |
| 9 | Alternative Movement | ~6h |

**Gesamt:** ~37 Stunden
