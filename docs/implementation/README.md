# Master-Implementierungsplan - Wiesenwischer GameKit

> **Letzte Aktualisierung:** 2026-02-11
> **Status:** In Entwicklung

---

## Übersicht

Dieser Plan ist der zentrale Einstiegspunkt für **alle** Epics und Features des GameKit-Projekts.
Jedes Epic gruppiert zusammengehörige Phasen. Jede Phase hat eigene Detail-Dokumente.

**Prinzipien:**
- Jede Phase = eigener Feature-Branch
- Jeder Schritt = eigener Commit
- Dokumentation vor Implementierung (`/plan-phase`)
- Implementierung Schritt für Schritt (`/impl-next`)
- Spezifikationen **müssen** vor Implementierung gelesen werden

**Slash Commands:**
- `/plan-phase` — Nächste nicht-ausgearbeitete Phase finden und detailliert ausarbeiten
- `/impl-next` — Aktuellen Fortschritt ermitteln und nächsten Schritt implementieren
- `/add-spec <pfad>` — Neue Spezifikation analysieren, in Phasen aufteilen und in den Plan aufnehmen

---

## Epic-Übersicht

| Epic | Phasen | Status |
|------|--------|--------|
| [Lebendige Charaktere — Animation Pipeline](#lebendige-charaktere--animation-pipeline) | 1–4, 20–22 | In Arbeit |
| [Fähigkeiten & Action Combat](#fähigkeiten--action-combat) | 5, 9 | Offen |
| [MMO-Netzwerk & Synchronisation](#mmo-netzwerk--synchronisation) | 6–7 | Offen |
| [Natürliche Bewegung — Inverse Kinematics](#natürliche-bewegung--inverse-kinematics) | 8 | Offen |
| [Reiten, Gleiten & Schwimmen](#reiten-gleiten--schwimmen) | 10 | Offen |
| [Character Platform](#character-platform) | 11–19 | Offen |

---

## Phasen-Übersicht (alle Epics)

| Phase | Epic | Name | Features | Ausgearbeitet | Status |
|-------|------|------|----------|---------------|--------|
| 1 | Animation | Animation-Vorbereitung | [Features](phase-1-animation-prep/README.md) | ✅ | Abgeschlossen |
| 2 | Animation | Animator Setup | [Features](phase-2-animator-setup/README.md) | ✅ | Abgeschlossen |
| 3 | Animation | Animation-Integration | [Features](phase-3-animation-integration/README.md) | ✅ | Abgeschlossen |
| 4 | Animation | Fortgeschrittene Lokomotion | [Features](phase-4-locomotion-features/README.md) | ✅ | Abgeschlossen |
| 5 | Combat | Ability System | [Features](phase-5-ability-system/README.md) | ✅ | Offen |
| 6 | Netzwerk | Netzwerk-Grundstruktur | — | ❌ | Offen |
| 7 | Netzwerk | Netzwerk-Animation | — | ❌ | Offen |
| 8 | IK | IK System | [Features](phase-8-ik-system/README.md) | ✅ | Abgeschlossen |
| 9 | Combat | Combat Abilities | — | ❌ | Offen |
| 10 | Movement | Alternative Movement | — | ❌ | Offen |
| 20 | Animation | Visual Grounding Smoother | [Features](phase-20-grounding-smoother/README.md) | ✅ | Offen |
| 21 | Animation | Slope Sliding | [Features](phase-21-slope-sliding/README.md) | ✅ | Offen |
| 22 | Animation | Landing Roll | [Features](phase-22-landing-roll/README.md) | ✅ | Offen |
| 11 | Character | CP: Core Data Model & Catalogs | — | ❌ | Offen |
| 12 | Character | CP: Builder Pipeline & Assembly Graph | — | ❌ | Offen |
| 13 | Character | CP: Equipment System | — | ❌ | Offen |
| 14 | Character | CP: Hair & Material System | — | ❌ | Offen |
| 15 | Character | CP: Body & Face System | — | ❌ | Offen |
| 16 | Character | CP: Creator UI & Scene | — | ❌ | Offen |
| 17 | Character | CP: Save/Load & Integration | — | ❌ | Offen |
| 18 | Character | CP: DNA Space & Constraints | — | ❌ | Offen |
| 19 | Character | CP: Morph Graph & HD Anatomy | — | ❌ | Offen |

---

## Abhängigkeiten

```
Lebendige Charaktere (Animation)
  Phase 1 ──> Phase 2 ──> Phase 3 ──> Phase 4 ──> Phase 21 (Slope Sliding)
                                                └──> Phase 22 (Landing Roll)

Fähigkeiten & Action Combat
  Phase 4 ──> Phase 5 ──> Phase 9

MMO-Netzwerk
  Phase 5 ──> Phase 6 ──> Phase 7

Natürliche Bewegung (IK)
  Phase 4 ──> Phase 20 (Grounding Smoother) ──> Phase 8

Reiten, Gleiten & Schwimmen
  Phase 6 ──> Phase 10

Character Platform (unabhängig von den anderen Epics)
  Phase 11 ──> Phase 12 ──> Phase 13
                   │
                   ├──> Phase 14 (Hair/Material)
                   └──> Phase 15 (Body/Face)
                            │
                            v
                        Phase 16 (UI) ──> Phase 17 (Save/Load)
                                               │
                                               v
                        Phase 18 (DNA Space & Constraints)
                                               │
                                               v
                        Phase 19 (Morph Graph & HD Anatomy)
```

**Hinweis:** Die Epics haben **keine feste Reihenfolge** untereinander. Die Reihenfolge ergibt sich aus den Phasen-Abhängigkeiten und der aktuellen Priorität. Insbesondere kann der Character Creator komplett parallel zu den anderen Epics entwickelt werden.

---

# Lebendige Charaktere — Animation Pipeline

Vom statischen Modell zum animierten Character: Assets vorbereiten, Animator mit Layer-System aufbauen, in den Character Controller integrieren und fortgeschrittene Locomotion-Features implementieren.

**Relevante Spezifikationen:**
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)
- [GameKit CharacterController Modular](../specs/GameKit_CharacterController_Modular.md)
- [AAA Action Combat & Character Architecture](../specs/AAA_Action_Combat_Character_Architecture.md)
- [Animation CrossFade Architektur](../specs/Animation_CrossFade_Architektur.md)

---

### Phase 1: Animation-Vorbereitung
**Branch:** `integration/phase-1-animation-prep`
**Ausgearbeitet:** ✅ Ja — [Detail-Dokument](phase-1-animation-prep/README.md)

**Schritte:**
- [x] [1.1 Character Asset beschaffen](phase-1-animation-prep/1.1-character-asset.md)
- [x] [1.2 Character in Unity importieren](phase-1-animation-prep/1.2-import-character.md)
- [x] [1.3 Basis-Animationen beschaffen](phase-1-animation-prep/1.3-animations.md)
- [x] [1.4 Animation Package Struktur](phase-1-animation-prep/1.4-package-structure.md)

---

### Phase 2: Animator Setup
**Branch:** `integration/phase-2-animator-setup`
**Ausgearbeitet:** ✅ Ja — [Detail-Dokument](phase-2-animator-setup/README.md)

**Schritte:**
- [x] [2.1 Avatar Masks erstellen](phase-2-animator-setup/2.1-avatar-masks.md)
- [x] [2.2 Animator Controller erstellen](phase-2-animator-setup/2.2-animator-controller.md)
- [x] [2.3 Locomotion Blend Tree](phase-2-animator-setup/2.3-locomotion-blend-tree.md)
- [x] [2.4 Airborne States](phase-2-animator-setup/2.4-airborne-states.md)
- [x] [2.5 Parameter-Bridge](phase-2-animator-setup/2.5-parameter-bridge.md)

---

### Phase 3: Animation-Integration
**Branch:** `integration/phase-3-animation-integration`
**Ausgearbeitet:** ✅ Ja — [Detail-Dokument](phase-3-animation-integration/README.md)

**Relevante Spezifikationen:**
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)
- [AAA Action Combat & Character Architecture](../specs/AAA_Action_Combat_Character_Architecture.md)
- [GameKit CharacterController Modular](../specs/GameKit_CharacterController_Modular.md)

**Schritte:**
- [x] [3.1 PlayerController Animation-Anbindung](phase-3-animation-integration/3.1-controller-binding.md)
- [x] [3.2 State Animation-Trigger](phase-3-animation-integration/3.2-state-animation-triggers.md)
- [x] [3.3 Player Prefab zusammenbauen](phase-3-animation-integration/3.3-player-prefab.md)
- [x] [3.4 Test-Szene & Verifikation](phase-3-animation-integration/3.4-test-verification.md)

---

### Phase 4: Fortgeschrittene Lokomotion
**Branch:** `integration/phase-4-locomotion-features`
**Ausgearbeitet:** ✅ Ja — [Detail-Dokument](phase-4-locomotion-features/README.md)

**Relevante Spezifikationen:**
- [Animation CrossFade Architektur](../specs/Animation_CrossFade_Architektur.md)
- [GameKit CharacterController Modular](../specs/GameKit_CharacterController_Modular.md)

**Schritte:**
- [x] 4.1 Stopping States (Light/Medium/Hard Deceleration mit Animationen)
- [x] 4.2 Landing System (Soft/Hard Landing mit konfigurierbaren Thresholds)
- [x] 4.3 Walk Toggle (MMO-Style, Y-Taste)
- [x] 4.4 Slope Speed Modifiers (Uphill Penalty, Downhill Bonus)
- [x] 4.5 Air Movement (AirControl, AirDrag, MinFallDistance)
- [x] 4.6 Detection Strategies (Ground/Fall, Motor/Collider-basiert)
- [x] 4.7 Stair Speed Reduction (Step-Frequenz-Erkennung)
- [x] 4.8 Ledge & Ground Snapping Config
- [x] 4.9 Animation CrossFade-System + TransitionConfig

---

### Phase 20: Visual Grounding Smoother
**Branch:** `integration/phase-20-grounding-smoother`
**Ausgearbeitet:** ✅ Ja — [Detail-Dokument](phase-20-grounding-smoother/README.md)

**Ziel:** Visuelles Y-Smoothing für Step-Ups (Treppen/Kanten) — Mesh gleitet per SmoothDamp statt diskret zu springen. Voraussetzung für Foot IK (Phase 8).

**Relevante Spezifikationen:**
- [GroundingSmoother Spezifikation](../specs/GroundingSmoother_Spezifikation.md)

**Schritte:**
- [ ] [20.1 GroundingSmoother Komponente](phase-20-grounding-smoother/20.1-grounding-smoother-component.md)
- [ ] [20.2 Unit Tests](phase-20-grounding-smoother/20.2-unit-tests.md)
- [ ] [20.3 Prefab-Integration](phase-20-grounding-smoother/20.3-prefab-integration.md)
- [ ] [20.4 Verifikation](phase-20-grounding-smoother/20.4-verification.md)

---

### Phase 21: Slope Sliding
**Branch:** `integration/phase-21-slope-sliding`
**Ausgearbeitet:** ✅ Ja — [Detail-Dokument](phase-21-slope-sliding/README.md)

**Ziel:** Dedizierter Sliding-State für steile Hänge — aktive Rutsch-Kraft statt passiver Projektion. Aktiviert die bisher ungenutzten `SlopeSlideSpeed`-Config-Werte und `SlopeModule.CalculateSlideVelocity()`.

**Relevante Spezifikationen:**
- [Slope Sliding Spezifikation](../specs/SlopeSliding_Spezifikation.md)

**Schritte:**
- [ ] [21.1 Slide-Animation beschaffen + importieren](phase-21-slope-sliding/21.1-slide-animation-asset.md)
- [x] [21.2 Config-Erweiterung (Sliding-Parameter)](phase-21-slope-sliding/21.2-config-erweiterung.md)
- [x] [21.3 Slide-Intent in CharacterLocomotion](phase-21-slope-sliding/21.3-locomotion-slide-intent.md)
- [x] [21.4 PlayerSlidingState implementieren](phase-21-slope-sliding/21.4-sliding-state.md)
- [x] [21.5 Entry/Exit-Transitions (Grounded, Falling)](phase-21-slope-sliding/21.5-entry-exit-transitions.md)
- [x] [21.6 Animation-Integration (Enum, Animator, CrossFade)](phase-21-slope-sliding/21.6-animation-integration.md)
- [x] [21.7 Unit Tests](phase-21-slope-sliding/21.7-unit-tests.md)
- [ ] [21.8 Play Mode Verifikation](phase-21-slope-sliding/21.8-play-mode-verifikation.md)

---

### Phase 22: Landing Roll
**Branch:** `integration/phase-22-landing-roll`
**Ausgearbeitet:** ✅ Ja — [Detail-Dokument](phase-22-landing-roll/README.md)

**Ziel:** Neuer Landing-State für Roll bei hartem Aufprall mit Movement-Input — Character rollt in Bewegungsrichtung statt komplett zu stoppen (HardLanding). Konfigurierbarer Trigger-Modus (MovementInput / ButtonPress), omni-direktional.

**Relevante Spezifikationen:**
- [Landing Roll Spezifikation](../specs/LandingRoll_Spezifikation.md)

**Schritte:**
- [ ] [22.1 Roll-Animation beschaffen + importieren](phase-22-landing-roll/22.1-roll-animation-asset.md)
- [ ] [22.2 RollTriggerMode Enum + Config-Erweiterung](phase-22-landing-roll/22.2-config-erweiterung.md)
- [ ] [22.3 CharacterAnimationState.Roll + Animator-State](phase-22-landing-roll/22.3-animation-integration.md)
- [ ] [22.4 PlayerRollingState implementieren](phase-22-landing-roll/22.4-rolling-state.md)
- [ ] [22.5 StateMachine + FallingState Transition](phase-22-landing-roll/22.5-statemachine-transition.md)
- [ ] [22.6 Unit Tests](phase-22-landing-roll/22.6-unit-tests.md)
- [ ] [22.7 Play Mode Verifikation](phase-22-landing-roll/22.7-play-mode-verifikation.md)

---

# Fähigkeiten & Action Combat

Modulares Ability-Framework als Infrastruktur für Nahkampf, Fernkampf, Zauber und Utility-Fähigkeiten im Action-Combat-Stil. Jump/Sprint bleiben Movement States — das Ability System ist eine orthogonale Schicht für Actions.

**Relevante Spezifikationen:**
- [Modulare Fertigkeiten Controller v2](../specs/ModularFertigkeitenController_Spezifikation_v2.md)
- [Skills & Action Combat](../specs/GameKit_Skills_ActionCombat.md)
- [AAA Action Combat & Character Architecture](../specs/AAA_Action_Combat_Character_Architecture.md)

---

### Phase 5: Ability System
**Branch:** `integration/phase-5-ability-system`
**Ausgearbeitet:** ✅ Ja — [Detail-Dokument](phase-5-ability-system/README.md)

**Schritte:**
- [ ] [5.1 Package-Struktur & IAbility Interface](phase-5-ability-system/5.1-package-iability-interface.md)
- [ ] [5.2 AbilityDefinition & AbilityContext](phase-5-ability-system/5.2-ability-definition-context.md)
- [ ] [5.3 AbilitySystem Manager](phase-5-ability-system/5.3-ability-system-manager.md)
- [ ] [5.4 Animation Layer Integration](phase-5-ability-system/5.4-animation-layer-integration.md)
- [ ] [5.5 PlayerController Integration & Tests](phase-5-ability-system/5.5-integration-tests.md)

---

### Phase 9: Combat Abilities
**Branch:** `integration/phase-9-combat-abilities`
**Ausgearbeitet:** ❌ Nein

**Schritte (vorläufig):**
- [ ] 9.1 Combat Package Struktur
- [ ] 9.2 MeleeAbility (Nahkampf)
- [ ] 9.3 RangedAbility (Fernkampf/Bogen)
- [ ] 9.4 SpellAbility (Zauber)
- [ ] 9.5 Combat Animationen
- [ ] 9.6 Combat Netzwerk-Sync

---

# MMO-Netzwerk & Synchronisation

FishNet-Integration für Multiplayer: Input- und Positions-Sync, Client-Side Prediction und Animations-Synchronisation über das Netzwerk.

**Relevante Spezifikationen:**
- [CSP Spezifikation](../specs/CSP_Spezifikation.md)
- [GameKit MMO Basics](../specs/GameKit_MMO_Basics.md)
- [GameKit InputSystem Spezifikation](../specs/GameKit_InputSystem_Spezifikation.md)
- [Master Architecture Overview](../specs/Wiesenwischer_Gamekit_Master_Architecture.md)

---

### Phase 6: Netzwerk-Grundstruktur
**Branch:** `integration/phase-6-network`
**Ausgearbeitet:** ❌ Nein

**Schritte (vorläufig):**
- [ ] 6.1 FishNet einbinden
- [ ] 6.2 NetworkPlayer
- [ ] 6.3 Input Sync
- [ ] 6.4 Position/Rotation Sync
- [ ] 6.5 Client-Side Prediction

---

### Phase 7: Netzwerk-Animation
**Branch:** `integration/phase-7-network-animation`
**Ausgearbeitet:** ❌ Nein

**Schritte (vorläufig):**
- [ ] 7.1 Animator Sync
- [ ] 7.2 State Sync
- [ ] 7.3 Ability Sync
- [ ] 7.4 Lag Compensation

---

# Natürliche Bewegung — Inverse Kinematics

Charaktere blicken Zielen nach, Füße passen sich dem Terrain an und Hände greifen Objekte — durch LookAt-IK, Foot Placement und Hand-IK.

**Relevante Spezifikationen:**
- [GameKit IK Spezifikation](../specs/GameKit_IK_Spezifikation.md)
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)

---

### Phase 8: IK System
**Branch:** `integration/phase-8-ik-system`
**Ausgearbeitet:** ✅ Ja — [Detail-Dokument](phase-8-ik-system/README.md)

**Relevante Spezifikationen:**
- [GameKit IK Spezifikation](../specs/GameKit_IK_Spezifikation.md)
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)

**Schritte:**
- [x] [8.1 Package-Struktur & IIKModule Interface](phase-8-ik-system/8.1-package-interfaces.md)
- [x] [8.2 IKManager Komponente](phase-8-ik-system/8.2-ik-manager.md)
- [x] [8.3 FootIK Modul](phase-8-ik-system/8.3-foot-ik.md)
- [x] [8.4 LookAtIK Modul](phase-8-ik-system/8.4-lookat-ik.md)
- [x] [8.5 Prefab-Integration & Tests](phase-8-ik-system/8.5-integration-tests.md)

---

# Reiten, Gleiten & Schwimmen

Alternative Fortbewegungsarten mit eigenen Movement Controllern, Animationen und Netzwerk-Sync.

**Relevante Spezifikationen:**
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)
- [GameKit CharacterController Modular](../specs/GameKit_CharacterController_Modular.md)
- [AAA Action Combat & Character Architecture](../specs/AAA_Action_Combat_Character_Architecture.md)

---

### Phase 10: Alternative Movement
**Branch:** `integration/phase-10-alternative-movement`
**Ausgearbeitet:** ❌ Nein

**Schritte (vorläufig):**
- [ ] 10.1 Movement Controller Abstraktion
- [ ] 10.2 RidingController (Reiten)
- [ ] 10.3 GlidingController (Gleiten)
- [ ] 10.4 SwimmingController (Schwimmen)
- [ ] 10.5 Movement Animationen
- [ ] 10.6 Movement Netzwerk-Sync

---

# Character Platform

AAA-taugliche Character Platform mit Live-Preview Creator, Equipment System, DNA-basiertem Appearance Model und deterministischer Assembly Pipeline — MMO-ready.

**Haupt-Spezifikation (konsolidiert):**
- [Character Platform Specification](../specs/Wiesenwischer_GameKit_CharacterPlatform_Specification.md)

**Quell-Spezifikationen:**
- [AAA Studio Technical Design Document v8](../specs/Wiesenwischer_GameKit_AAA_CharacterSystem_TechDesignDoc_v8.md)
- [Character Creator System Specification v1.3](../specs/Wiesenwischer_GameKit_CharacterCreator_Specification.md)

**Packages:**
- `Wiesenwischer.GameKit.Character.Core` — AppearanceDNA, Constraints, Mapping, Interfaces, Services
- `Wiesenwischer.GameKit.Character.Content` — Kataloge (SO), Presets, Mapping Tables, Icons
- `Wiesenwischer.GameKit.Character.Runtime` — Assembly Graph, Builder Pipeline, Drivers, Components
- `Wiesenwischer.GameKit.Character.Creator` — Creator UI, Preview Proxy, State, Camera
- `Wiesenwischer.GameKit.Character.Demo` — Demo-Scene, Sample-Content

---

### Phase 11: CP Core Data Model & Catalogs
**Branch:** `integration/phase-11-cp-data-model`
**Ausgearbeitet:** ❌ Nein

**Ziel:** AppearanceDNA-Datenmodell und Katalog-System als Grundlage für alles weitere.

**Vorläufige Schritte:**
- [ ] 11.1 Package-Struktur anlegen (`Character.Core`, `Character.Content`)
- [ ] 11.2 `AppearanceDNA` + Sub-DNA-Klassen (Body, Face, Skin, Eye, Equipment) + `SerializableColor`
- [ ] 11.3 `EquipmentSlot` Enum und Grundtypen
- [ ] 11.4 ScriptableObject-Kataloge (`HairCatalog`, `EquipmentCatalog`, `FacePresetCatalog`)
- [ ] 11.5 `CatalogProvider` Service (Katalog-Zugriff zur Runtime)
- [ ] 11.6 Mapping Layer Grundstruktur (`BlendShapeMap`, `BoneMap`, `MaterialParamMap`)
- [ ] 11.7 Unit Tests für Datenmodell

**Referenz:** Konsolidierte Spec Kapitel 4, 5, 6

---

### Phase 12: CP Builder Pipeline & Assembly Graph
**Branch:** `integration/phase-12-cp-builder`
**Ausgearbeitet:** ❌ Nein

**Ziel:** Assembly Graph mit Dirty Tracking und deterministische Build-Pipeline.

**Package:** `Character.Runtime`

**Vorläufige Schritte:**
- [ ] 12.1 `BoneMapCache` (Bone-Name → Transform Lookup)
- [ ] 12.2 Assembly Graph Grundgerüst (Nodes, Dirty Tracking, Partial Rebuild)
- [ ] 12.3 `CharacterBuilder` mit Pipeline-Steps
- [ ] 12.4 Pipeline Step: Resolve Assets
- [ ] 12.5 Pipeline Step: Ensure Base Character
- [ ] 12.6 Pipeline Step: Skeleton Mapping
- [ ] 12.7 Pipeline Step: Post-Fix (Bounds, Validation)
- [ ] 12.8 Unit Tests für Builder & Graph

**Referenz:** Konsolidierte Spec Kapitel 7, 8, 9

---

### Phase 13: CP Equipment System
**Branch:** `integration/phase-13-cp-equipment`
**Ausgearbeitet:** ❌ Nein

**Ziel:** Slot-basiertes Equipment-System mit SkinnedMesh Bone-Rebind.

**Vorläufige Schritte:**
- [ ] 13.1 `EquipmentBinder` (Bone Rebind by Name)
- [ ] 13.2 Pipeline Step: Equip Equipment (EquipmentNode)
- [ ] 13.3 Body Masking / Hide-Under-Cloth (Hide BlendShapes)
- [ ] 13.4 Compatibility Metadata + Bone Coverage Validation
- [ ] 13.5 Integration Tests mit Test-Prefabs

**Referenz:** Konsolidierte Spec Kapitel 14

---

### Phase 14: CP Hair & Material System
**Branch:** `integration/phase-14-cp-hair-material`
**Ausgearbeitet:** ❌ Nein

**Ziel:** Hair-Prefab-System und Runtime-Material-Coloring via MaterialPropertyBlock.

**Vorläufige Schritte:**
- [ ] 14.1 Pipeline Step: Equip Hair (HairNode, Prefab + Bone Rebind)
- [ ] 14.2 `HairColorApplier` mit MaterialPropertyBlock
- [ ] 14.3 `HairShaderAdapter` (Shader-Property-Mapping)
- [ ] 14.4 Pipeline Step: Apply Materials (MaterialNode, Skin/Eyes/Hair)
- [ ] 14.5 Skin Melanin Slider + Tattoo Toggle
- [ ] 14.6 Tests

**Referenz:** Konsolidierte Spec Kapitel 13, 15

---

### Phase 15: CP Body & Face System
**Branch:** `integration/phase-15-cp-body-face`
**Ausgearbeitet:** ❌ Nein

**Ziel:** Bone-driven Body Sliders (MVP) und Face-Preset-System.

**Vorläufige Schritte:**
- [ ] 15.1 Pipeline Step: Apply Body (BodyRatioNode, Bone Scaling)
- [ ] 15.2 Bone-Scaling-Config (Breast, Butt, Leg Length, Height, ShoulderWidth)
- [ ] 15.3 Safety Rules (Scaling Ranges, Default Restore, keine Twist-Bones)
- [ ] 15.4 Pipeline Step: Apply Face (FaceMorphNode, Mesh Variant Swap)
- [ ] 15.5 Face Morph Fallback (optional, wenn BlendShapes vorhanden)
- [ ] 15.6 Eye System Basis (Iris Color + Pupil Size via MPB)
- [ ] 15.7 Tests

**Referenz:** Konsolidierte Spec Kapitel 10, 11, 12

---

### Phase 16: CP Creator UI & Scene
**Branch:** `integration/phase-16-cp-ui`
**Ausgearbeitet:** ❌ Nein

**Ziel:** CharacterCreatorScene mit Preview Proxy Pattern, Live-Preview und Tab-basiertem UI.

**Packages:** `Character.Creator`, `Character.Demo`

**Vorläufige Schritte:**
- [ ] 16.1 Creator Package-Struktur anlegen
- [ ] 16.2 Preview Proxy Pattern (Creator vs Gameplay Trennung)
- [ ] 16.3 `CreatorState` (aktive DNA, Dirty Flags, Undo History, Debounce)
- [ ] 16.4 `CreatorBootstrapper` + Scene-Hierarchie (Lighting, Environment)
- [ ] 16.5 FacePanel (Grid mit Presets)
- [ ] 16.6 BodyPanel (Sliders)
- [ ] 16.7 HairPanel (Liste + Color Picker)
- [ ] 16.8 OutfitPanel (Slot-Listen)
- [ ] 16.9 Camera Focus Logic (Cinemachine)
- [ ] 16.10 FinalizePanel

**Referenz:** Konsolidierte Spec Kapitel 18

---

### Phase 17: CP Save/Load & Integration
**Branch:** `integration/phase-17-cp-saveload`
**Ausgearbeitet:** ❌ Nein

**Ziel:** JSON-Persistenz, Versionierung und Anbindung an das restliche GameKit.

**Vorläufige Schritte:**
- [ ] 17.1 JSON Serialisierung/Deserialisierung von `AppearanceDNA`
- [ ] 17.2 DNA `version` + Migration-Strategie
- [ ] 17.3 Preset-Management (Custom Presets speichern/laden)
- [ ] 17.4 Integration: Character aus DNA im Gameplay spawnen
- [ ] 17.5 Addressables-Vorbereitung (optional)
- [ ] 17.6 End-to-End Tests

**Referenz:** Konsolidierte Spec Kapitel 19

---

### Phase 18: CP DNA Space & Constraints (Post-MVP)
**Branch:** `integration/phase-18-cp-dna-space`
**Ausgearbeitet:** ❌ Nein

**Ziel:** DNA Space Modell für Preset Mixing, Constraints für Realismus und NPC Variation.

**Vorläufige Schritte:**
- [ ] 18.1 DNA Space Modell (Parameter-Vektor, Preset als Punkt)
- [ ] 18.2 Preset Mixing (Lerp/Choose/Blend per Channel-Typ)
- [ ] 18.3 Constraint System Grundgerüst (Range, Dependency, Soft Correction)
- [ ] 18.4 Constraint Execution Pipeline (Apply → Clamp → Constrain → Emit)
- [ ] 18.5 NPC Randomization (Sample Space + Constrain)
- [ ] 18.6 Tests

**Referenz:** Konsolidierte Spec Kapitel 16

---

### Phase 19: CP Morph Graph & HD Anatomy (Post-MVP)
**Branch:** `integration/phase-19-cp-morph-graph`
**Ausgearbeitet:** ❌ Nein

**Ziel:** Morph Graph für AAA-Qualität Body/Face, HD Anatomy BlendShapes und erweiterte Systeme.

**Vorläufige Schritte:**
- [ ] 19.1 Mapping Layer erweitern (BlendShapeMap, BoneMap, MaterialParamMap als SO)
- [ ] 19.2 Morph Graph (DNA Channels → Drivers, Curves, Normalization)
- [ ] 19.3 HD Anatomy BlendShape Integration (BodyMorphNode)
- [ ] 19.4 Advanced Face Morphs (FaceMorphNode mit BlendShapes)
- [ ] 19.5 Aging System (strukturell + Material)
- [ ] 19.6 Eye System erweitert (Shape Sliders, Eyelid Bones)
- [ ] 19.7 Performance Tuning + LOD Integration
- [ ] 19.8 Tests

**Referenz:** Konsolidierte Spec Kapitel 17

---

# Workflow

### Neue Phase planen
```
/plan-phase
```
Findet automatisch die nächste Phase ohne ausgearbeitete Detail-Docs und erstellt diese.

### Nächsten Schritt implementieren
```
/impl-next
```
Ermittelt den aktuellen Fortschritt und implementiert den nächsten offenen Schritt.

### Neue Spec aufnehmen
```
/add-spec <pfad-zur-spec>
```
Analysiert eine Spezifikation, teilt sie in Phasen auf und integriert ein neues Epic in diesen Plan.

### Branch-Workflow

**Phase starten (Integration-Branch):**
```bash
git checkout main && git pull origin main
git checkout -b integration/phase-X-beschreibung
git push -u origin integration/phase-X-beschreibung
```

**Schritt implementieren (Feature-Branch, kurzlebig):**
```bash
git checkout integration/phase-X-beschreibung
git checkout -b feat/fachliche-beschreibung
# Entwickeln, committen...
git push -u origin feat/fachliche-beschreibung
gh pr create --base integration/phase-X-beschreibung --title "feat: Beschreibung"
```

**Phase abschließen (→ main):**
```bash
gh pr create --base main --title "feat: Phase X - Beschreibung"
# Nach Review: Merge, Integration-Branch löschen
```

### Commit-Format
```
feat(phase-X): X.Y Beschreibung
```
