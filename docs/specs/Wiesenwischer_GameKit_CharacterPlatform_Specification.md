# Wiesenwischer GameKit — Character Platform Specification

**Version:** 1.0 (Konsolidiert)
**Date:** 2026-02-09
**Quellen:**
- AAA Studio Technical Design Document v8
- Character Creator System Specification v1.3

**Target Engine:** Unity 2022 LTS (HDRP)
**Character Pipeline:** Character Creator 5 (CC5)
**Goal:** AAA-quality modular Character Platform + Equipment System

**System Overview:** ![Character Platform System Overview](../CharactorCreator_System_Overview.png)

---

## 0. Executive Summary

Wir bauen eine **Character Platform** (nicht nur einen "Character Creator") die unterstützt:

- **Player Character Creation** (UI-driven Customization)
- **NPC Generation / Population Diversity**
- **Modular Equipment** (Hair, Armor, Accessories)
- **AAA-Rendering** via CC5 Auto Setup Shaders (HDRP)
- **Deterministic Reconstruction** via **AppearanceDNA** (Serialisierung + Multiplayer Readiness)
- **Scalability** durch **Assembly Graph** und **Dependency-based Rebuild**

### Key Decisions

1. **AppearanceDNA** ist die **Single Source of Truth** (SSOT)
2. **Preview Proxy** Pattern: Creator und Gameplay-Character sind getrennt
3. **Assembly Graph**: Dependency-basiert, dirty-marking, partial rebuild
4. **Body MVP**: Bone-driven Sliders (stabil mit Animation + Armor)
5. **Body Target**: HD Anatomy BlendShapes via Blender-Pipeline (wenn verfügbar)
6. **Face MVP**: Preset/Variant-basiert (Mesh Swap)
7. **Face Target**: Layered Hybrid (BlendShapes + Bones + Materials)
8. **Materials**: Immer MaterialPropertyBlock (kein Material-Instancing)
9. **Equipment**: Slot-basiert + Runtime Bone Rebind by Name

---

## 1. Scope & Goals

### 1.1 Must-Have (MVP)

- Character Creator UI Scene mit Live-Preview
- Face Preset Selection (Grid)
- Body Sliders: Breast, Butt, Leg Length (bone-driven)
- Hair Selection + Runtime Hair Color (MPB)
- 2–3 Equipment Slots (Chest/Pants/Boots)
- Save/Load Appearance als JSON
- Deterministic Builder Pipeline

### 1.2 Should-Have

- Camera Focus per Kategorie (Face/Body/Hair)
- Body Masking / Hide-Body-Parts
- Preset Management (Custom Presets)
- Addressables für Content Streaming
- Skin Tone (Melanin) + Tattoo Toggle

### 1.3 Target (Post-MVP)

- HD Anatomy Body BlendShapes (Blender Pipeline)
- Face Morph Sliders (wenn BlendShapes exportiert)
- DNA Space + Constraints + Variation Model
- Morph Graph + Mapping Layer
- Dual Graph (Authoring + Runtime Split)
- Eye System (Shape, Iris, Pupil, Wetness)
- Aging System (strukturell + Material)
- NPC Variation/Randomization
- LOD Integration

### 1.4 Non-Goals (v1)

- Vollständiges prozedurales Sculpting
- Player-importierte Meshes
- Photogrammetry Pipeline
- Runtime Cloth Simulation Authoring

---

## 2. Terminology

| Begriff | Definition |
|---------|-----------|
| **AppearanceDNA** | Autoritative Daten die das Aussehen in semantischen Parametern beschreiben |
| **DNA Space** | Parameterraum in dem jeder Character ein Punkt/Vektor ist |
| **Preset** | Ein benannter DNA-Punkt (oder partieller DNA) als Startkoordinate |
| **Offset** | Benutzer-Adjustierung auf einem Preset |
| **Morph Channel** | Semantischer Parameter (z.B. Body.Weight) der einen oder mehrere Driver beeinflusst |
| **Driver** | Mechanismus der DNA auf die Character-Instanz anwendet: BlendShapeDriver, BoneDriver, MaterialDriver, EquipmentDriver |
| **Catalog** | Liste verfügbarer Assets (Hair, Equipment, Face Presets) als ScriptableObject |
| **Assembly Graph** | Dependency Graph der nur betroffene Teile aktualisiert wenn DNA sich ändert |
| **Builder** | Deterministische Pipeline die aus DNA + Catalogs einen Character baut |
| **Preview Proxy** | Creator-only Visual Character Instance (getrennt von Gameplay) |
| **Partial Apply** | Nur geändertes Subsystem updaten (Hair only, Equipment only, etc.) |
| **Mapping Layer** | Abstraktion: semantische Keys → Engine-Parameter (BlendShape-Index, Bone-Transform, Shader-Property) |

---

## 3. Top-Level Architecture

```
Creator UI  ─┐
             ├→ AppearanceService → AppearanceDNA → [Constraints] → Assembly Graph → Preview Proxy
Presets/Var  ┘

Saved DNA / Network DNA → Assembly Graph → Builder Pipeline → Gameplay Character Instance
```

### Design Principles

1. **Data-driven**: Appearance = Daten, nie Prefab-Varianten
2. **SSOT**: Alles wird aus AppearanceDNA abgeleitet und ist re-derivable
3. **Single Skeleton + Animator**: Alle Varianten teilen das gleiche Rig
4. **Content Catalogs**: ScriptableObjects für Assets, nie Hart-Referenzen
5. **Deterministic Build**: Gleiche DNA + gleiche Catalogs = gleiches Ergebnis
6. **No direct UI→Mesh coupling**: UI modifiziert nur DNA via Service
7. **Modular Subsystems**: Face/Body/Hair/Equipment/Materials unabhängig

### Creator vs Gameplay Separation (Preview Proxy)

Creator und Gameplay teilen **nie** mutable state:
- Creator nutzt **PreviewProxy** (visual-only, keine Gameplay-Komponenten)
- Gameplay nutzt separate Runtime-Character (mit Animator, Controller, Network)
- DNA ist die Bridge zwischen beiden

---

## 4. Package Structure

```
Wiesenwischer.GameKit.Character.Core
  ├── DNA/              (AppearanceDNA, DNAVersion, Interfaces)
  ├── Constraints/      (IRangeConstraint, IDependencyConstraint)
  ├── Mapping/          (MappingLayer, BlendShapeMap, BoneMap, MaterialParamMap)
  ├── Services/         (IAppearanceService, ICatalogProvider)
  └── Utils/            (BoneMapCache, Hashing, Debounce)

Wiesenwischer.GameKit.Character.Content
  ├── Catalogs/         (HairCatalog, EquipmentCatalog, FacePresetCatalog, SkinCatalog)
  ├── Presets/          (Face Presets, Body Presets)
  ├── MappingTables/    (ScriptableObject Mapping-Daten)
  └── Icons/

Wiesenwischer.GameKit.Character.Runtime
  ├── Assembly/         (AssemblyGraph, AssemblyNode, DirtyTracking)
  ├── Builder/          (CharacterBuilder, Pipeline Steps)
  ├── Drivers/          (BlendShapeDriver, BoneDriver, MaterialDriver, EquipmentDriver)
  └── Components/       (CharacterApplier, EquipmentBinder, HairColorApplier)

Wiesenwischer.GameKit.Character.Creator
  ├── UI/               (Panels, Bindings, ViewModels, Widgets)
  ├── Preview/          (PreviewProxy, PreviewController)
  ├── State/            (CreatorState, UndoHistory)
  └── Camera/           (CreatorCameraController, FocusTargets)

Wiesenwischer.GameKit.Character.Demo
  ├── Scenes/           (CharacterCreatorScene.unity)
  └── SampleContent/
```

### Dependency Rules

- **Core** hat keine Abhängigkeiten zu anderen Character-Packages
- **Content** hängt von Core ab
- **Runtime** hängt von Core und Content ab
- **Creator** hängt von Core, Content und Runtime ab
- **Runtime darf NICHT von Creator abhängen**

---

## 5. AppearanceDNA (Domain Model)

### 5.1 DNA Structure

```csharp
[Serializable]
public sealed class AppearanceDNA
{
    public int version = 1;
    public string gender;           // "Female" | "Male"

    // Body
    public BodyDNA body;

    // Face
    public FaceDNA face;

    // Skin
    public SkinDNA skin;

    // Eyes
    public EyeDNA eyes;

    // Equipment
    public EquipmentDNA equipment;
}

[Serializable]
public sealed class BodyDNA
{
    // MVP (bone-driven)
    public float breastSize;        // 0..1
    public float buttSize;          // 0..1
    public float legLength;         // 0..1

    // Target (archetype blendshapes, wenn verfügbar)
    public float weight;            // 0..1
    public float muscle;            // 0..1
    public float fitness;           // 0..1 (female)

    // Ratio (bone-driven)
    public float height;            // 0..1
    public float shoulderWidth;     // 0..1
}

[Serializable]
public sealed class FaceDNA
{
    // MVP (preset-based)
    public string presetId;

    // Target (morph-based, wenn BlendShapes vorhanden)
    public float noseWidth;         // 0..1
    public float noseLength;        // 0..1
    public float eyeSize;           // 0..1
    public float eyeTilt;           // 0..1
    public float jawWidth;          // 0..1
    public float earSize;           // 0..1
    public string earTypeId;
    public string noseTypeId;
}

[Serializable]
public sealed class SkinDNA
{
    public float melanin;           // 0..1
    public float age;               // 0..1
    public float freckles;          // 0..1
    public string skinPresetId;
    public bool tattoosEnabled;
    public float tattooIntensity;   // 0..1
}

[Serializable]
public sealed class EyeDNA
{
    public SerializableColor irisColor;
    public string irisPatternId;
    public float pupilSize;         // 0..1
}

[Serializable]
public sealed class EquipmentDNA
{
    public string hairId;
    public SerializableColor hairBase;
    public SerializableColor hairStrandRoot;
    public SerializableColor hairHighlightA;
    public SerializableColor hairHighlightB;

    public string eyebrowId;
    public string beardId;

    public Dictionary<EquipmentSlot, string> equippedItemIds;
}

public enum EquipmentSlot
{
    Hair, Head, Chest, Pants, Boots, Gloves, Back
}
```

### 5.2 Design-Prinzipien

- **Nur semantische Werte** in DNA — nie Engine-Indices (BlendShape-Index, Material-Instance)
- **version** erlaubt Migrationen
- Felder die noch nicht implementiert sind → Default-Wert, werden ignoriert
- Alle Werte sind serialisierbar und stabil

### 5.3 SerializableColor

```csharp
[Serializable]
public struct SerializableColor
{
    public float r, g, b, a;

    public Color ToColor() => new Color(r, g, b, a);
    public static SerializableColor FromColor(Color c) =>
        new SerializableColor { r = c.r, g = c.g, b = c.b, a = c.a };
}
```

---

## 6. Content Catalog System

### 6.1 ScriptableObject Catalogs

```csharp
[CreateAssetMenu(menuName = "GameKit/Character/HairCatalog")]
public class HairCatalog : ScriptableObject
{
    public List<HairItem> items;
}

[Serializable]
public class HairItem
{
    public string id;
    public GameObject prefab;       // Skinned mesh hair prefab
    public Sprite icon;
    public bool supportsRuntimeColor;
}
```

Analog: `EquipmentCatalog`, `FacePresetCatalog`, `SkinCatalog`

### 6.2 Equipment Metadata

Jedes Equipment-Item trägt:
- `compatibleBaseIds` — kompatible Base-Character
- `hideBodyMask` — welche Body-Regionen versteckt werden
- `requiresBones` — Validierung der benötigten Bones

### 6.3 Mapping Tables (ScriptableObjects)

Abstrahieren Engine-spezifische Details:
- `BlendShapeMap`: semantischer Key → (RendererID, BlendShape-Name)
- `BoneMap`: semantischer Key → Bone-Transform-Name(s)
- `MaterialParamMap`: semantischer Key → Shader-Property-Name

Beispiel-Keys: `Body.Female.Athletic`, `Face.Nose.Width`, `Skin.Melanin`, `Eyes.IrisColor`

---

## 7. Character Prefab Contract

```
CharacterRoot
  SkeletonRoot (full bone hierarchy, CC_Base_BoneRoot)
  MeshRoot
    BodyRenderer (SkinnedMeshRenderer, inkl. Head bei CC5)
    EyesRenderer (optional separat)
    TeethRenderer (optional separat)
  Slots
    Slot_Hair
    Slot_Head
    Slot_Chest
    Slot_Pants
    Slot_Boots
    Slot_Gloves
    Slot_Back
```

Requirements:
- `Animator` + Avatar (Humanoid)
- Skeleton Root (`CC_Base_BoneRoot` oder equivalent)
- Primary `SkinnedMeshRenderer` für Body
- Slot Transforms für Equipment/Hair

---

## 8. Assembly Graph

### 8.1 Warum Graph statt Pipeline?

Pipeline (linear) rebuildet zu viel bei kleinen Änderungen. Graph erlaubt:
- **Dependency Ordering**
- **Caching** pro Node
- **Partial Rebuild** (nur dirty Nodes)
- **Async Support** (für Addressables Loading)

### 8.2 Graph Nodes

| Node | Inputs | Dependencies | Funktion |
|------|--------|-------------|----------|
| BaseCharacterNode | DNA.gender | — | Base-Character laden/sicherstellen |
| BodyMorphNode | DNA.body.weight/muscle/fitness | BaseCharacter | BlendShape-Weights setzen (wenn verfügbar) |
| BodyRatioNode | DNA.body.breastSize/buttSize/legLength/height | BaseCharacter | Bone Scaling anwenden |
| FaceMorphNode | DNA.face.* | BaseCharacter | Face Preset Swap oder Morph Weights |
| EquipmentNode | DNA.equipment.equippedItemIds | BaseCharacter | Equipment instantiate + bone rebind |
| HairNode | DNA.equipment.hairId | BaseCharacter | Hair instantiate + bone rebind |
| MaterialNode | DNA.skin.*, DNA.eyes.*, DNA.equipment.hairColors | Active Renderers | MPB params setzen |
| FinalizeNode | — | Alle | Bounds refresh, LOD, Validation |

### 8.3 Dirty Marking

Wenn ein DNA-Subset sich ändert, werden nur betroffene Nodes dirty:
- EyeColor ändert → nur MaterialNode
- Weight ändert → BodyMorphNode + EquipmentNode (Hide Masks) + FinalizeNode
- Hair wechseln → HairNode + MaterialNode + FinalizeNode

---

## 9. Builder Pipeline (Deterministic Apply)

### 9.1 Pipeline Steps (Reihenfolge)

1. **Resolve Assets** — IDs → Prefabs/Presets aus Catalogs
2. **Ensure Base Character** — Instanz prüfen/erstellen
3. **Skeleton Mapping Cache** — Bone-Name → Transform Lookup
4. **Apply Face** — MVP: Mesh Variant Swap; Target: Morph Weights
5. **Apply Body** — Bone Scaling (Breast/Butt/Leg) + optional BlendShapes
6. **Equip Hair** — Prefab instantiate + Bone Rebind
7. **Equip Equipment** — Pro Slot: Prefab instantiate + Bone Rebind + Body Masking
8. **Apply Materials** — MPB für Hair Color, Skin, Eyes
9. **Post-Fix** — Bounds update, Missing Bones validieren, LOD

### 9.2 Determinismus

Gleiche `AppearanceDNA` + gleiche Catalogs → identisches Ergebnis.

---

## 10. Body System

### 10.1 MVP: Bone-Driven Sliders

Stabil mit Animation und Armor. Keine BlendShape-Abhängigkeit.

| Parameter | Bones | Scaling | Range |
|-----------|-------|---------|-------|
| breastSize | Upper Torso / Spine02 | XZ | 0.90–1.18 |
| buttSize | Pelvis / Waist | XZ | 0.90–1.15 |
| legLength | Thigh + Calf Y-Scale | Y | 0.95–1.05 |
| height | Overall Scale | XYZ | 0.95–1.05 |
| shoulderWidth | Clavicle / Upper Torso | X | 0.95–1.08 |

**Safety Rules:**
- Nie Twist/Capsule/Share-Bones skalieren
- Konservative Ranges
- Default Scales pro Bone speichern und wiederherstellen

### 10.2 Target: HD Anatomy BlendShapes

HD Anatomy Archetypes als Basis:
- Female: Athletic, Fit, Heavy, Plump, Slim
- Male: Athletic, Bodybuilder, Drudge, Elder, Heavy, Skinny, Slim

**Blender Pipeline:**
1. Export Base (neutral) aus CC5
2. Export jedes Archetype (gleiche Topologie)
3. In Blender: Base Mesh bekommt Shape Keys von jedem Archetype
4. Export eine FBX mit allen Shape Keys

**Body Ratio bleibt bone-driven** (separate Schicht):
- Vermeidet Geometry-Distortions die Armor brechen
- Konsistent für IK und Animation

---

## 11. Face System

### 11.1 MVP: Preset/Variant-Based

CC5 exportiert oft Expressions/Visemes aber **nicht** volle Head-Shape Morph Sliders.
Daher: Face Selection ist preset/variant-basiert.

**Option A (empfohlen): Mesh Variant Swap**
- Ein Skeleton + Animator beibehalten
- Body SkinnedMesh (inkl. Head) mit Variante austauschen
- Bones per Name rebinden

**Option B: Morph Preset Apply**
- Nur wenn Face Morphs als BlendShapes exportiert
- Morph Weights per Name/Index setzen

### 11.2 Target: Layered Hybrid (AAA)

1. **Face Archetypes** (12): Starting Points (BlendShapes)
2. **Structural Morphs**: Face Proportions (BlendShapes)
3. **Feature Morphs**: Nose/Eyes/Ears/Jaw (BlendShapes)
4. **Micro Adjustments**: Eyelid, Eye Direction (Face Bones)
5. **Materials**: Skin/Eyes/Makeup/Aging Overlays

---

## 12. Eye System

### 12.1 Components

- **Eye Shape**: Head Morphs (Size, Spacing, Tilt, Depth)
- **Eyelids**: Morph oder Bone Micro (Blink Base Openness)
- **Iris Color**: Material Parameter (MPB)
- **Pupil Size**: Shader Param
- **Wetness/Specular**: Eye Shader Params (optional)

### 12.2 MVP

Eye Color/Pupil via MPB. Shape als Teil des Face Presets.

### 12.3 Target

Separate Eye Shape Sliders + Eyelid Bones für "live" Behaviors (Blink/Look).

---

## 13. Skin & Aging

### 13.1 CC5 Skin Shader (HDRP)

Nutze CC5 Auto Setup Shader Stack. Kein eigener Shader nötig.

Runtime-Parameter via MPB:
- Skin Tint / Melanin Proxy
- SSS Intensity/Profile
- Wrinkle Intensity / Mask Strength
- Roughness Modifier
- Detail Normal Strength

### 13.2 Aging (AAA Two-Part)

Aging ist nicht nur Wrinkles:
1. **Structural Aging** (BlendShapes): Eye Bags, Sagging, Folds
2. **Material Aging**: Wrinkle Map Strength, Roughness, Color Shifts

Age Slider treibt beides via Morph Graph + Material Drivers.

### 13.3 MVP

Skin Preset Selection + Tattoo Toggle. Melanin-Slider.

---

## 14. Equipment System

### 14.1 Slot-Based Design

Jeder Slot hat 0..1 Item. Slots: Hair, Head, Chest, Pants, Boots, Gloves, Back.

### 14.2 Bone Rebind by Name

```
1. Instantiate Item Prefab unter Slot Root
2. Build BoneMap vom Character Rig
3. Rebind jede SkinnedMeshRenderer:
   - rootBone zuweisen
   - bones[] Array per Name-Lookup rebuilden
4. Parent unter Slot Transform
```

### 14.3 Body Masking / Hide Under-Cloth

Per-Item `hideBodyMask`: Liste von Body-Regionen/BlendShapes die auf 100 gesetzt werden.

MVP: CC-side Hidden-Body Exports für große Outfits.
Target: Runtime Hide-BlendShapes oder Material Mask.

### 14.4 Failure Modes & Guardrails

- Bind Pose Mismatch → Deformation Artifacts
- Bone Name Mismatch → Invisible Mesh
- Extra Armature Root → Strip/Disable bei Runtime
- Validierung: Bone Coverage Check bei Import (Editor Utility)

---

## 15. Hair System

### 15.1 Runtime Hair Color via MPB

`MaterialPropertyBlock` per Renderer. Nie shared Materials global mutieren.

Hair Parameters (Shader-abhängig):
- Base Color
- Strand/Root Color
- Highlights A/B

### 15.2 HairShaderAdapter

Kennt die Property-Namen für den gewählten Shader. Bei unterschiedlichen Shaders zwischen Hairs wählt der Adapter das richtige Mapping.

---

## 16. DNA Space & Constraints (Post-MVP)

### 16.1 DNA Space

DNA ist ein Vektor. Presets sind Punkte. Sliders bewegen den Punkt.
- Preset Mixing = lineare Algebra (`DNA = A*(1-t) + B*t` per Channel)
- Random Generation = "sample space then constrain"
- Channel-spezifische Mixing Rules (Floats: Lerp, IDs: Choose, One-Hot: Blend+Renormalize)

### 16.2 Constraints

| Typ | Beschreibung |
|-----|-------------|
| Range | Clamp per Channel |
| Dependency | Relationships erzwingen (soft oder hard) |
| Region | Ungültige Kombinationen blocken (selten) |
| Soft Correction | Subtile Auto-Kompensation (AAA-Style) |

**Execution Order:**
1. Direkte User-Änderung auf DNA
2. Range Clamps
3. Dependency Constraints (soft)
4. Region Constraints (selten)
5. Final corrected DNA emittieren

**Wichtig:** Constraints werden **vor** Graph Dirty-Marking angewendet (kein Preview-Flicker).

### 16.3 Beispiel-Constraints

- `body.weight` > 0.8 → max `face.jaw.width` reduzieren
- `skin.age` > 0.7 → max `eyes.size` leicht reduzieren
- `ratio.legLength` steigt → `ratio.height` auto-adjustieren

---

## 17. Morph Graph & Mapping Layer (Post-MVP)

### 17.1 Morph Graph

Internes Mapping von DNA Channels zu Drivers. Nicht der Assembly Graph.

Beispiel: `Body.Weight` beeinflusst:
- `F_Slim`, `F_Plump`, `F_Heavy` mit Curve + Normalization
- Auto-Kompensation bestimmter Bereiche

Implementierung: Mapping Rules (Curves) + Dependency Functions, getrieben von DNA Inputs.

### 17.2 Mapping Layer

Nie Engine-Indices in DNA oder UI einbetten.

| Map | Key (semantisch) | Value (Engine) |
|-----|-------------------|----------------|
| BlendShapeMap | `Body.Female.Athletic` | (rendererIndex, blendShapeName) |
| BoneMap | `Body.Ratio.LegLength` | bone transform name(s) |
| MaterialParamMap | `Skin.Melanin` | shader property name |

Mapping Layer wird aus ScriptableObjects im Content Package geladen.

---

## 18. Scene Architecture (Creator)

```
CharacterCreatorScene
  Systems
    CreatorBootstrapper
    CatalogProvider
    SaveLoadController
  Preview
    PreviewProxy (visual-only character instance)
    FocusTargets
      Focus_Head
      Focus_Torso
      Focus_Legs
  Cameras
    CinemachineBrain
    CM_OrbitCamera
  Environment (neutral backdrop)
  Lighting (HDRP volume, key/fill/rim, reflection probe)
  UI
    Canvas
      Tabs
        FacePanel
        BodyPanel
        HairPanel
        OutfitPanel
        ColorPanel
        FinalizePanel
```

### UI Rules

UI modifiziert **nur** AppearanceDNA via AppearanceService. UI berührt **nie** direkt:
- BlendShape Weights
- Materials
- Equipment GameObjects

### Camera Focus

| Tab | Focus Target |
|-----|-------------|
| Face | Focus_Head |
| Body | Focus_Torso / Focus_Legs |
| Hair | Focus_Head |
| Outfits | Focus_Torso |

---

## 19. Save/Load & Versioning

### 19.1 JSON Format

Serialisiere nur AppearanceDNA als JSON. Stabile Feldnamen.

### 19.2 Migration

- `version` in DNA
- Bei Load: wenn DNA Version < aktuelle → migrieren (fehlende Felder mit Defaults)
- Mapping Tables separat halten — nie Indices in DNA "baken"

### 19.3 Workflow

- Creator: Edit DNA → Preview aktualisiert → Save Button → DNA JSON
- Gameplay: Load DNA JSON → Spawn Runtime Character → Apply via Assembly Graph

---

## 20. Performance

- **MPB** statt Material Instancing (immer)
- **BlendShape Updates**: Batch per Frame, dirty list, nur geänderte Weights setzen
- **Throttling**: Optional 30Hz während Slider Drag, 60Hz bei Release
- **LODGroup** per Character (optional separate LOD für Hair/Accessories)
- **Addressables** für große Content-Mengen (Hair/Outfits/Face Variants)
- **Partial Apply** statt Full Rebuild bei jeder Änderung

---

## 21. Implementation Phases

### Phase 10: Core Data Model & Catalogs (MVP Foundation)

- Package-Struktur: `Character.Core`, `Character.Content`
- AppearanceDNA + SerializableColor
- EquipmentSlot Enum und Grundtypen
- SO-Kataloge: HairCatalog, EquipmentCatalog, FacePresetCatalog
- CatalogProvider Service
- Unit Tests

### Phase 11: Builder Pipeline & Bone System

- BoneMapCache (Name → Transform)
- CharacterBuilder Grundgerüst mit Pipeline Steps
- Assembly Graph (Nodes, Dirty Tracking, Partial Rebuild)
- Pipeline Steps: Resolve, Ensure Base, Skeleton Mapping, Post-Fix
- Unit Tests

### Phase 12: Equipment System

- EquipmentBinder (Bone Rebind by Name)
- Pipeline Step: Equip Equipment
- Body Masking / Hide-Under-Cloth (Basis)
- Compatibility Metadata
- Integration Tests

### Phase 13: Hair & Material System

- Pipeline Step: Equip Hair (Prefab + Bone Rebind)
- HairColorApplier mit MPB
- HairShaderAdapter
- Pipeline Step: Apply Materials (Skin/Eyes/Hair)
- Skin Melanin + Tattoo Toggle
- Tests

### Phase 14: Body & Face System

- Pipeline Step: Apply Body (Bone Scaling)
- Bone-Scaling-Config (Breast, Butt, Leg Length)
- Safety Rules (Ranges, Default Restore)
- Pipeline Step: Apply Face (Mesh Variant Swap)
- Face Morph Fallback (wenn BlendShapes vorhanden)
- Tests

### Phase 15: Creator UI & Scene

- UI Package: `Character.Creator`
- PreviewProxy Pattern (Creator vs Gameplay Trennung)
- CreatorState + Undo History
- CreatorBootstrapper + Scene-Hierarchie
- Panels: Face, Body, Hair, Outfit, Finalize
- Camera Focus Logic (Cinemachine)
- Debounced Partial Apply

### Phase 16: Save/Load & Integration

- JSON Serialisierung/Deserialisierung
- DNA Version + Migration
- Preset Management
- Integration: Character aus DNA im Gameplay spawnen
- Addressables-Vorbereitung
- End-to-End Tests

### Phase 17: DNA Space & Constraints (Post-MVP)

- DNA Space Modell
- Preset Mixing (Lerp/Choose/Blend)
- Constraint System (Range, Dependency, Soft Correction)
- Constraint Execution Pipeline
- NPC Randomization Basics
- Tests

### Phase 18: Morph Graph & HD Anatomy (Post-MVP)

- Mapping Layer (BlendShapeMap, BoneMap, MaterialParamMap)
- Morph Graph (DNA Channels → Drivers, Curves, Normalization)
- HD Anatomy BlendShape Integration (wenn Blender Pipeline ready)
- Advanced Face Morphs
- Aging System (structural + material)
- Eye System Details
- Performance Tuning + LOD
- Tests

---

## Appendix A: HD Anatomy Archetype List

**Female:** Athletic, Fit, Heavy, Plump, Slim
**Male:** Athletic, Bodybuilder, Drudge, Elder, Heavy, Skinny, Slim

Jeder Archetype-Export muss vom gleichen neutralen Base-Character per Gender abgeleitet sein (gleiche Topologie).

## Appendix B: Empfohlenes Initial Slider Set (MVP)

**Body:** Breast Size, Butt Size, Leg Length
**Face:** Face Preset Select (Grid)
**Hair:** Hair Select + Color Pickers (Base, Root, Highlight A/B)
**Skin:** Melanin (Tone), Tattoo Toggle
**Equipment:** 2–3 Slots (Chest, Pants, Boots)

## Appendix C: Creator UX Rules

- Preset Selection setzt Base DNA Koordinate
- Sliders wenden Offsets auf dem Preset an
- "Reset Section" setzt nur Offsets zurück (Preset bleibt)
- Undo/Redo speichert DNA Snapshots
- Live Preview mit Throttled Updates während Drag

---

END OF SPECIFICATION
