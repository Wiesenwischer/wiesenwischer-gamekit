# Master-Implementierungsplan - Wiesenwischer GameKit

> **Letzte Aktualisierung:** 2026-02-09
> **Status:** In Planung

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
| [Lebendige Charaktere — Animation Pipeline](#lebendige-charaktere--animation-pipeline) | 1–3 | Offen |
| [Fähigkeiten & Action Combat](#fähigkeiten--action-combat) | 4, 8 | Offen |
| [MMO-Netzwerk & Synchronisation](#mmo-netzwerk--synchronisation) | 5–6 | Offen |
| [Natürliche Bewegung — Inverse Kinematics](#natürliche-bewegung--inverse-kinematics) | 7 | Offen |
| [Reiten, Gleiten & Schwimmen](#reiten-gleiten--schwimmen) | 9 | Offen |
| [Character Creator & Ausrüstung](#character-creator--ausrüstung) | 10–16 | Offen |

---

## Phasen-Übersicht (alle Epics)

| Phase | Epic | Name | Features | Ausgearbeitet | Status |
|-------|------|------|----------|---------------|--------|
| 1 | Animation | Animation-Vorbereitung | [Features](phase-1-animation-prep/README.md) | ✅ | Abgeschlossen |
| 2 | Animation | Animator Setup | [Features](phase-2-animator-setup/README.md) | ✅ | Offen |
| 3 | Animation | Animation-Integration | [Features](phase-3-animation-integration/README.md) | ✅ | Offen |
| 4 | Combat | Ability System | — | ❌ | Offen |
| 5 | Netzwerk | Netzwerk-Grundstruktur | — | ❌ | Offen |
| 6 | Netzwerk | Netzwerk-Animation | — | ❌ | Offen |
| 7 | IK | IK System | — | ❌ | Offen |
| 8 | Combat | Combat Abilities | — | ❌ | Offen |
| 9 | Movement | Alternative Movement | — | ❌ | Offen |
| 10 | Creator | CC: Core Data Model & Catalogs | — | ❌ | Offen |
| 11 | Creator | CC: Builder Pipeline & Bone System | — | ❌ | Offen |
| 12 | Creator | CC: Equipment System | — | ❌ | Offen |
| 13 | Creator | CC: Hair & Material System | — | ❌ | Offen |
| 14 | Creator | CC: Body & Face System | — | ❌ | Offen |
| 15 | Creator | CC: Creator UI & Scene | — | ❌ | Offen |
| 16 | Creator | CC: Save/Load & Integration | — | ❌ | Offen |

---

## Abhängigkeiten

```
Lebendige Charaktere (Animation)
  Phase 1 ──> Phase 2 ──> Phase 3

Fähigkeiten & Action Combat
  Phase 3 ──> Phase 4 ──> Phase 8

MMO-Netzwerk
  Phase 4 ──> Phase 5 ──> Phase 6

Natürliche Bewegung (IK)
  Phase 3 ──> Phase 7

Reiten, Gleiten & Schwimmen
  Phase 5 ──> Phase 9

Character Creator & Ausrüstung (unabhängig von den anderen Epics)
  Phase 10 ──> Phase 11 ──> Phase 12
                   │
                   ├──> Phase 13 (Hair/Material)
                   └──> Phase 14 (Body/Face)
                            │
                            v
                        Phase 15 (UI) ──> Phase 16 (Save/Load)
```

**Hinweis:** Die Epics haben **keine feste Reihenfolge** untereinander. Die Reihenfolge ergibt sich aus den Phasen-Abhängigkeiten und der aktuellen Priorität. Insbesondere kann der Character Creator komplett parallel zu den anderen Epics entwickelt werden.

---

# Lebendige Charaktere — Animation Pipeline

Vom statischen Modell zum animierten Character: Assets vorbereiten, Animator mit Layer-System aufbauen und in den Character Controller integrieren.

**Relevante Spezifikationen:**
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)
- [GameKit CharacterController Modular](../specs/GameKit_CharacterController_Modular.md)
- [AAA Action Combat & Character Architecture](../specs/AAA_Action_Combat_Character_Architecture.md)

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
- [ ] [2.3 Locomotion Blend Tree](phase-2-animator-setup/2.3-locomotion-blend-tree.md)
- [ ] [2.4 Airborne States](phase-2-animator-setup/2.4-airborne-states.md)
- [ ] [2.5 Parameter-Bridge](phase-2-animator-setup/2.5-parameter-bridge.md)

---

### Phase 3: Animation-Integration
**Branch:** `integration/phase-3-animation-integration`
**Ausgearbeitet:** ✅ Ja — [Detail-Dokument](phase-3-animation-integration/README.md)

**Relevante Spezifikationen:**
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)
- [AAA Action Combat & Character Architecture](../specs/AAA_Action_Combat_Character_Architecture.md)
- [GameKit CharacterController Modular](../specs/GameKit_CharacterController_Modular.md)

**Schritte:**
- [ ] [3.1 PlayerController Animation-Anbindung](phase-3-animation-integration/3.1-controller-binding.md)
- [ ] [3.2 State Animation-Trigger](phase-3-animation-integration/3.2-state-animation-triggers.md)
- [ ] [3.3 Player Prefab zusammenbauen](phase-3-animation-integration/3.3-player-prefab.md)
- [ ] [3.4 Test-Szene & Verifikation](phase-3-animation-integration/3.4-test-verification.md)

---

# Fähigkeiten & Action Combat

Modulares Ability-System als Grundgerüst für Jump, Sprint und darauf aufbauend Nahkampf, Fernkampf und Zauber im Action-Combat-Stil.

**Relevante Spezifikationen:**
- [Modulare Fertigkeiten Controller v2](../specs/ModularFertigkeitenController_Spezifikation_v2.md)
- [Skills & Action Combat](../specs/GameKit_Skills_ActionCombat.md)
- [AAA Action Combat & Character Architecture](../specs/AAA_Action_Combat_Character_Architecture.md)

---

### Phase 4: Ability System
**Branch:** `integration/phase-4-ability-system`
**Ausgearbeitet:** ❌ Nein

**Schritte (vorläufig):**
- [ ] 4.1 IAbility Interface
- [ ] 4.2 AbilitySystem Manager
- [ ] 4.3 JumpAbility
- [ ] 4.4 SprintAbility
- [ ] 4.5 Animation Layer Integration

---

### Phase 8: Combat Abilities
**Branch:** `integration/phase-8-combat-abilities`
**Ausgearbeitet:** ❌ Nein

**Schritte (vorläufig):**
- [ ] 8.1 Combat Package Struktur
- [ ] 8.2 MeleeAbility (Nahkampf)
- [ ] 8.3 RangedAbility (Fernkampf/Bogen)
- [ ] 8.4 SpellAbility (Zauber)
- [ ] 8.5 Combat Animationen
- [ ] 8.6 Combat Netzwerk-Sync

---

# MMO-Netzwerk & Synchronisation

FishNet-Integration für Multiplayer: Input- und Positions-Sync, Client-Side Prediction und Animations-Synchronisation über das Netzwerk.

**Relevante Spezifikationen:**
- [CSP Spezifikation](../specs/CSP_Spezifikation.md)
- [GameKit MMO Basics](../specs/GameKit_MMO_Basics.md)
- [GameKit InputSystem Spezifikation](../specs/GameKit_InputSystem_Spezifikation.md)
- [Master Architecture Overview](../specs/Wiesenwischer_Gamekit_Master_Architecture.md)

---

### Phase 5: Netzwerk-Grundstruktur
**Branch:** `integration/phase-5-network`
**Ausgearbeitet:** ❌ Nein

**Schritte (vorläufig):**
- [ ] 5.1 FishNet einbinden
- [ ] 5.2 NetworkPlayer
- [ ] 5.3 Input Sync
- [ ] 5.4 Position/Rotation Sync
- [ ] 5.5 Client-Side Prediction

---

### Phase 6: Netzwerk-Animation
**Branch:** `integration/phase-6-network-animation`
**Ausgearbeitet:** ❌ Nein

**Schritte (vorläufig):**
- [ ] 6.1 Animator Sync
- [ ] 6.2 State Sync
- [ ] 6.3 Ability Sync
- [ ] 6.4 Lag Compensation

---

# Natürliche Bewegung — Inverse Kinematics

Charaktere blicken Zielen nach, Füße passen sich dem Terrain an und Hände greifen Objekte — durch LookAt-IK, Foot Placement und Hand-IK.

**Relevante Spezifikationen:**
- [GameKit IK Spezifikation](../specs/GameKit_IK_Spezifikation.md)
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)

---

### Phase 7: IK System
**Branch:** `integration/phase-7-ik-system`
**Ausgearbeitet:** ❌ Nein

**Schritte (vorläufig):**
- [ ] 7.1 IK Package Struktur
- [ ] 7.2 IKManager Komponente
- [ ] 7.3 LookAtIK Implementation
- [ ] 7.4 FootIK Implementation
- [ ] 7.5 HandIK Implementation
- [ ] 7.6 IK Netzwerk-Sync

---

# Reiten, Gleiten & Schwimmen

Alternative Fortbewegungsarten mit eigenen Movement Controllern, Animationen und Netzwerk-Sync.

**Relevante Spezifikationen:**
- [Animationskonzept LayeredAbilities](../specs/Animationskonzept_LayeredAbilities.md)
- [GameKit CharacterController Modular](../specs/GameKit_CharacterController_Modular.md)
- [AAA Action Combat & Character Architecture](../specs/AAA_Action_Combat_Character_Architecture.md)

---

### Phase 9: Alternative Movement
**Branch:** `integration/phase-9-alternative-movement`
**Ausgearbeitet:** ❌ Nein

**Schritte (vorläufig):**
- [ ] 9.1 Movement Controller Abstraktion
- [ ] 9.2 RidingController (Reiten)
- [ ] 9.3 GlidingController (Gleiten)
- [ ] 9.4 SwimmingController (Schwimmen)
- [ ] 9.5 Movement Animationen
- [ ] 9.6 Movement Netzwerk-Sync

---

# Character Creator & Ausrüstung

AAA-tauglicher Character Creator mit Live-Preview: Gesicht, Körper, Haare und Ausrüstung anpassen — datengetrieben, deterministisch und MMO-ready.

**Haupt-Spezifikation:**
- [Character Creator System Specification](../specs/Wiesenwischer_GameKit_CharacterCreator_Specification.md)

**Packages:**
- `Wiesenwischer.GameKit.CharacterCreator.Core` — Datenmodell, Builder, Assembly, Services
- `Wiesenwischer.GameKit.CharacterCreator.Content` — Kataloge, Presets, Icons
- `Wiesenwischer.GameKit.CharacterCreator.UI` — Panels, Bindings, ViewModels
- `Wiesenwischer.GameKit.CharacterCreator.Demo` — Demo-Scene, Sample-Content

---

### Phase 10: CC Core Data Model & Catalogs
**Branch:** `integration/phase-10-cc-data-model`
**Ausgearbeitet:** ❌ Nein

**Ziel:** Datenmodell und Katalog-System als Grundlage für alles weitere.

**Vorläufige Schritte:**
- [ ] 10.1 Package-Struktur anlegen (`CharacterCreator.Core`, `.Content`)
- [ ] 10.2 `CharacterAppearance` + `SerializableColor` implementieren
- [ ] 10.3 `EquipmentSlot` Enum und Grundtypen
- [ ] 10.4 ScriptableObject-Kataloge (`HairCatalog`, `EquipmentCatalog`, `FacePresetCatalog`)
- [ ] 10.5 `CatalogProvider` Service (Katalog-Zugriff zur Runtime)
- [ ] 10.6 Unit Tests für Datenmodell

**Referenz:** Spec Kapitel 4, 7, 8

---

### Phase 11: CC Builder Pipeline & Bone System
**Branch:** `integration/phase-11-cc-builder`
**Ausgearbeitet:** ❌ Nein

**Ziel:** Deterministische Build-Pipeline, die aus Appearance-Daten einen Character aufbaut.

**Vorläufige Schritte:**
- [ ] 11.1 `BoneMapCache` (Bone-Name → Transform Lookup)
- [ ] 11.2 `CharacterBuilder` Grundgerüst mit Pipeline-Steps
- [ ] 11.3 Pipeline Step: Resolve Assets
- [ ] 11.4 Pipeline Step: Ensure Base Character
- [ ] 11.5 Pipeline Step: Skeleton Mapping
- [ ] 11.6 Pipeline Step: Post-Fix (Bounds, Validation)
- [ ] 11.7 Unit Tests für Builder

**Referenz:** Spec Kapitel 6, 10

---

### Phase 12: CC Equipment System
**Branch:** `integration/phase-12-cc-equipment`
**Ausgearbeitet:** ❌ Nein

**Ziel:** Slot-basiertes Equipment-System mit SkinnedMesh Bone-Rebind.

**Vorläufige Schritte:**
- [ ] 12.1 `EquipmentBinder` (Bone Rebind by Name)
- [ ] 12.2 Pipeline Step: Equip Equipment
- [ ] 12.3 Body Masking / Hide-Under-Cloth (Basis)
- [ ] 12.4 Compatibility Metadata auf Items
- [ ] 12.5 Integration Tests mit Test-Prefabs

**Referenz:** Spec Kapitel 13

---

### Phase 13: CC Hair & Material System
**Branch:** `integration/phase-13-cc-hair-material`
**Ausgearbeitet:** ❌ Nein

**Ziel:** Hair-Prefab-System und Runtime-Material-Coloring via MaterialPropertyBlock.

**Vorläufige Schritte:**
- [ ] 13.1 Pipeline Step: Equip Hair (Prefab + Bone Rebind)
- [ ] 13.2 `HairColorApplier` mit MaterialPropertyBlock
- [ ] 13.3 `HairShaderAdapter` (Shader-Property-Mapping)
- [ ] 13.4 Pipeline Step: Apply Materials
- [ ] 13.5 Skin/Tattoo Toggle (optional)
- [ ] 13.6 Tests

**Referenz:** Spec Kapitel 14, 15

---

### Phase 14: CC Body & Face System
**Branch:** `integration/phase-14-cc-body-face`
**Ausgearbeitet:** ❌ Nein

**Ziel:** Bone-driven Body Sliders und Face-Preset-System.

**Vorläufige Schritte:**
- [ ] 14.1 Pipeline Step: Apply Body (Bone Scaling)
- [ ] 14.2 Bone-Scaling-Config (Breast, Butt, Leg Length)
- [ ] 14.3 Safety Rules (Scaling Ranges, Default Restore)
- [ ] 14.4 Pipeline Step: Apply Face (Mesh Variant Swap)
- [ ] 14.5 Face Morph Fallback (optional, wenn Blendshapes vorhanden)
- [ ] 14.6 Tests

**Referenz:** Spec Kapitel 11, 12

---

### Phase 15: CC Creator UI & Scene
**Branch:** `integration/phase-15-cc-ui`
**Ausgearbeitet:** ❌ Nein

**Ziel:** CharacterCreatorScene mit Live-Preview und Tab-basiertem UI.

**Packages:** `CharacterCreator.UI`, `CharacterCreator.Demo`

**Vorläufige Schritte:**
- [ ] 15.1 UI Package-Struktur anlegen
- [ ] 15.2 `CreatorState` (aktive Appearance, Dirty Flags, Debounce)
- [ ] 15.3 `CreatorBootstrapper` + Scene-Hierarchie
- [ ] 15.4 FacePanel (Grid mit Presets)
- [ ] 15.5 BodyPanel (Sliders)
- [ ] 15.6 HairPanel (Liste + Color Picker)
- [ ] 15.7 OutfitPanel (Slot-Listen)
- [ ] 15.8 Camera Focus Logic (Cinemachine)
- [ ] 15.9 FinalizePanel

**Referenz:** Spec Kapitel 5, 16

---

### Phase 16: CC Save/Load & Integration
**Branch:** `integration/phase-16-cc-saveload`
**Ausgearbeitet:** ❌ Nein

**Ziel:** JSON-Persistenz, Versionierung und Anbindung an das restliche GameKit.

**Vorläufige Schritte:**
- [ ] 16.1 JSON Serialisierung/Deserialisierung von `CharacterAppearance`
- [ ] 16.2 `appearanceVersion` + Migration-Strategie
- [ ] 16.3 Preset-Management (Custom Presets speichern/laden)
- [ ] 16.4 Integration: Character aus Appearance im Gameplay spawnen
- [ ] 16.5 Addressables-Vorbereitung (optional)
- [ ] 16.6 End-to-End Tests

**Referenz:** Spec Kapitel 17, 18

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
