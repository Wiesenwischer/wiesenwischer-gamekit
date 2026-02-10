# Wiesenwischer GameKit --- Character Creator System Specification

Version: **1.3 (PRO)**\
Date: 2026-02-09\
Target Engine: **Unity 2022 LTS (HDRP)** *(last LTS before Unity 6)*\
Character Pipeline: **Character Creator 5 (CC5)**\
Goal: **AAA-quality modular runtime Character Creator + Equipment
system**\
Primary integration target: **Wiesenwischer GameKit**

------------------------------------------------------------------------

## 0. Executive Summary

We build a **data-driven**, **modular** character customization system
with a dedicated **CharacterCreatorScene** UI application. The runtime
character is assembled by a **Character Assembly Graph** and applied
through a deterministic **CharacterBuilder** pipeline.

Key decisions:

-   **Face**: primarily via **CC5 presets / ActorMixer assets** and/or
    mesh variants (because CC5 often exports expressions but not full
    morph sliders).
-   **Body shape (runtime)**: primarily via **bone-driven sliders**
    (stable with animation + armor); blendshapes only when reliably
    exported.
-   **Hair/skin colors**: **runtime material parameters** using
    **MaterialPropertyBlock** (no material instancing per character).
-   **Equipment**: slot-based modular **SkinnedMeshRenderer** items
    bound to the same skeleton (bone remap by name).
-   **Save/Load**: store **appearance data only** (JSON). Never save
    prefabs.

------------------------------------------------------------------------

# 1. Scope & Goals

## 1.1 Must-Have Features (MVP)

-   Character Creator UI scene with live preview
-   Select **Face preset** (list/grid)
-   Sliders:
    -   **Breast size**
    -   **Butt size**
    -   **Leg length**
-   Select **Hair** (asset list)
-   Change **Hair color** at runtime
-   Equip at least 2--3 **equipment slots** (e.g., Chest/Pants/Boots)
-   Save/Load character appearance to JSON

## 1.2 Should-Have Features

-   Camera focus by category (face/body/hair)
-   Body masking/hide-body-parts to reduce clipping
-   Preset management (save custom presets)
-   Asynchronous content loading (Addressables)

## 1.3 Constraints / Non-Goals (initially)

-   No fully free-form Skyrim-style facial sculpting unless we
    explicitly export/create morph targets.
-   No automatic cloth simulation authoring (optional later: Magica
    Cloth / Unity cloth).

------------------------------------------------------------------------

# 2. Terminology

-   **Appearance**: pure data describing the character (IDs, floats,
    colors).
-   **Catalog**: list of available assets (hair, equipment, face
    presets).
-   **Assembly Graph**: resolves assets + dependencies and orchestrates
    build order.
-   **Builder**: executes deterministic application (mesh swap, bone
    scaling, material params).
-   **Partial Apply**: apply only changed subsystem (hair only,
    equipment only, etc.).

------------------------------------------------------------------------

# 3. High-Level Architecture

    [UI Panels] ──> [CreatorState (Appearance)] ──> [CharacterBuilder]
                                             └──> [Save/Load JSON]
    [Catalogs] ──> [Assembly Graph + Resolvers] ──> [Builder Pipeline]

Design principles: - **Data-driven** - **Single skeleton + animator** -
**Content catalogs (ScriptableObjects)** - **Deterministic build** -
**No direct UI→mesh coupling** - **Modular subsystems**
(Face/Body/Hair/Equipment/Materials)

------------------------------------------------------------------------

# 4. Package / Folder Structure (GameKit-aligned)

Recommended packages (or top-level folders if not using UPM packages
yet):

-   `Wiesenwischer.GameKit.CharacterCreator.Core`
    -   `Appearance/` (data model, enums)
    -   `Builder/` (builder + pipeline steps)
    -   `Assembly/` (graph, resolvers)
    -   `Services/` (interfaces, runtime services)
    -   `Utils/` (bone mapping, hashing, debounce, etc.)
-   `Wiesenwischer.GameKit.CharacterCreator.Content`
    -   `Catalogs/` (ScriptableObject catalogs)
    -   `Presets/` (Face presets, Body presets)
    -   `Icons/`
-   `Wiesenwischer.GameKit.CharacterCreator.UI`
    -   `Panels/`
    -   `Bindings/`
    -   `ViewModels/` (light MVVM)
    -   `Widgets/`
-   `Wiesenwischer.GameKit.CharacterCreator.Demo`
    -   `Scenes/CharacterCreatorScene.unity`
    -   demo assets + sample catalogs

------------------------------------------------------------------------

# 5. Scene Architecture

## 5.1 CharacterCreatorScene Hierarchy (recommended)

    CharacterCreatorScene
      Systems
        CreatorBootstrapper
        CatalogProvider
        SaveLoadController
      Preview
        CharacterPreviewRoot
          CharacterRoot (instantiated prefab)
          FocusTargets
            Focus_Head
            Focus_Torso
            Focus_Legs
      Cameras
        CinemachineBrain
        CM_OrbitCamera (FreeLook or Orbital)
      Lighting (HDRP)
        Volume_Global
        KeyLight / FillLight / RimLight
      UI
        Canvas
          Tabs
            FacePanel
            BodyPanel
            HairPanel
            OutfitPanel
            ColorPanel
            FinalizePanel

## 5.2 Camera Rules

-   Selecting a tab triggers camera focus:
    -   Face → Focus_Head
    -   Body → Focus_Torso / Focus_Legs
    -   Hair → Focus_Head
    -   Outfits → Focus_Torso

------------------------------------------------------------------------

# 6. Character Prefab Contract

The base character prefab must provide:

-   `Animator` + Avatar (Humanoid)
-   Skeleton root (`CC_Base_BoneRoot` or equivalent)
-   A primary `SkinnedMeshRenderer` for the body (often includes head in
    CC5)
-   A `MeshRoot` transform where variants can be swapped/attached
-   Slot transforms for equipment/hair (or a consistent locator pattern)

Recommended structure:

    CharacterRoot
      SkeletonRoot (contains full bone hierarchy)
      MeshRoot
        BodyRenderer (SkinnedMeshRenderer)
        EyesRenderer (optional separate)
        TeethRenderer (optional separate)
      Slots
        Slot_Hair
        Slot_Head
        Slot_Chest
        Slot_Pants
        Slot_Boots
        Slot_Gloves
        Slot_Back

------------------------------------------------------------------------

# 7. Appearance Data Model (Claude-ready)

## 7.1 Core Types

``` csharp
public enum EquipmentSlot
{
    Hair, Head, Chest, Pants, Boots, Gloves, Back
}

[Serializable]
public sealed class CharacterAppearance
{
    public string baseCharacterId;

    // Face
    public string facePresetId; // preferred
    public Dictionary<string, float> faceMorphs; // optional/advanced

    // Body (runtime)
    public float breastSize01;  // 0..1
    public float buttSize01;    // 0..1
    public float legLength01;   // 0..1

    // Hair
    public string hairId;
    public SerializableColor hairBase;
    public SerializableColor hairStrandRoot;
    public SerializableColor hairHighlightA;
    public SerializableColor hairHighlightB;

    // Equipment
    public Dictionary<EquipmentSlot, string> equippedItemIds;

    // Skin / decals
    public string skinPresetId;         // optional
    public bool tattoosEnabled;         // optional
    public float tattooIntensity01;     // optional
}
```

Notes: - Keep all values serializable and stable. - Use IDs referencing
catalogs, not direct asset references.

------------------------------------------------------------------------

# 8. Content Catalog System

## 8.1 ScriptableObject Catalogs

``` csharp
[CreateAssetMenu(menuName="GameKit/CharacterCreator/HairCatalog")]
public class HairCatalog : ScriptableObject
{
    public List<HairItem> items;
}

[Serializable]
public class HairItem
{
    public string id;
    public GameObject prefab; // Skinned mesh hair prefab
    public Sprite icon;
    public bool supportsRuntimeColor;
}
```

Similarly: - `EquipmentCatalog` - `FacePresetCatalog` - `SkinCatalog`
(optional)

## 8.2 Compatibility Metadata

Equipment items should carry: - `compatibleBaseIds` (list) OR a rule
like `CC_Base` generation marker - `hideBodyMask` (per-slot mask
hints) - `requiresBones` (validation)

------------------------------------------------------------------------

# 9. Character Assembly Graph (PRO)

## 9.1 Why we need it

Without a graph, customization becomes spaghetti: - UI triggers random
rebuild steps - race conditions with async loading - equipment and
materials apply out of order

The graph provides: - dependency ordering - caching - partial rebuild -
async support

## 9.2 Graph Model (concept)

Nodes: - BaseCharacterNode - FaceNode - BodyNode - HairNode -
EquipmentNode (per slot) - MaterialNode - PostProcessNode

Edges: - Hair depends on BaseSkeleton - Equipment depends on
BaseSkeleton - Material depends on active renderers

## 9.3 Partial Apply Rules

When appearance changes: - if only `hairBase` changed → update hair
material only - if only `equippedItemIds[Chest]` changed → rebuild chest
slot only - if face preset changed → swap body mesh variant or apply
morph preset (depending on pipeline)

------------------------------------------------------------------------

# 10. Character Builder (Deterministic Apply Pipeline)

## 10.1 Pipeline Steps (recommended order)

1.  **Resolve assets** from catalogs (IDs → prefabs/presets)
2.  **Ensure base character** exists and is prepared
3.  **Skeleton mapping cache** (bone name → transform)
4.  **Apply Face**
    -   Preferred: apply CC5 face preset via variant mesh swap
    -   Optional: apply morph weights if morphs exist
5.  **Apply Body**
    -   Bone-driven scaling sliders (breast/butt/leg length)
6.  **Equip Hair**
    -   Instantiate hair prefab under Slot_Hair
    -   Rebind bones by name
7.  **Equip Equipment**
    -   For each slot: instantiate prefab, rebind bones
    -   Apply body masking rules
8.  **Apply Materials**
    -   Hair color using MaterialPropertyBlock
    -   Skin overlays (tattoo on/off) if available
9.  **Post-fix**
    -   Update bounds
    -   Validate missing bones
    -   Optional: LOD/renderer settings

## 10.2 Determinism

Given the same `CharacterAppearance` and the same catalogs, the output
must be identical.

------------------------------------------------------------------------

# 11. Face System (CC5 Reality + Strategy)

## 11.1 CC5 Reality Check

In Unity imports you often get: - expression / viseme blendshapes but
NOT: - full head-shape morph sliders

Therefore: **Face selection is usually preset/variant-based**, not free
sculpting.

## 11.2 Authoring Workflow (CC5)

-   Build/select a set of high-quality faces using:
    -   Store characters
    -   ActorMixer / Create Mixer Assets
-   Save face presets for reuse.
-   Export **Face Variants** as FBX for Unity.

## 11.3 Runtime Implementation Options

**Option A (recommended): Mesh Variant Swap** - Keep one skeleton +
animator - Swap the body skinned mesh (which includes head) using a
variant source (same bone names) - Rebind bones by name

**Option B: Morph Preset Apply** - Only if the face morphs are truly
exported as blendshapes - Apply morph weights by name/index

------------------------------------------------------------------------

# 12. Body System (Breast / Butt / Leg Length)

## 12.1 Preferred: Bone-driven (stable with armor)

We do NOT rely on body blendshapes unless verified.

Recommended bones (CC naming examples): - Breast size: usually affects
upper torso; use `Spine02` XZ scaling (subtle) - Butt size: `Pelvis` /
`Waist` XZ scaling (subtle) - Leg length: - MVP: root Y scale (simple,
stable) - PRO: thigh + calf Y scale + foot offset compensation

## 12.2 Safety Rules

-   Never scale twist/capsule/share bones.
-   Keep scaling ranges conservative (e.g. 0.90..1.18).
-   Store and restore default scales per bone.

------------------------------------------------------------------------

# 13. Equipment System (Slots + Bone Rebind)

## 13.1 Slot-based design

Each slot can have 0..1 item equipped.

## 13.2 Bone Rebind by Name

When equipping a skinned item exported from CC5, it may contain its own
Armature root. We bind its SkinnedMeshRenderer bones to the character
skeleton by name.

Key behaviour: - Instantiate item prefab under slot root - Find item
SkinnedMeshRenderer(s) - Map `bones[]` to character bones using cached
lookup - Set `rootBone` to character pelvis/hip root bone as needed

## 13.3 Body Masking / Hide Under-Cloth

To reduce clipping: - Provide per-item mask hints (e.g., hide thighs
under pants) - Implement either: - CC-side hidden mesh export variants,
or - Unity-side renderer submesh toggles / mask meshes

MVP recommendation: start with CC-side hidden-body exports for major
outfits.

------------------------------------------------------------------------

# 14. Hair System (Runtime Coloring)

## 14.1 Runtime Hair Color

Use **MaterialPropertyBlock** per renderer.

Do NOT mutate shared materials globally.

Hair parameters (names vary by shader): - Base color - Strand/root
color - Highlights A/B

Implementation detail: - Provide a `HairShaderAdapter` that knows the
property names for the chosen shader(s). - If shader differs between
hairs, adapter chooses the right mapping.

------------------------------------------------------------------------

# 15. Tattoos / SkinGen (Toggle in Unity)

If tattoos are baked into textures: - simplest approach: two skin
presets (tattoo on/off)

If using HDRP: - consider **Decal Projector** overlays (preferred
long-term) - or layered mask in skin shader

Expose in appearance: - `tattoosEnabled` - `tattooIntensity01`
(optional)

------------------------------------------------------------------------

# 16. UI Architecture (Creator App)

## 16.1 State

`CreatorState` holds: - active `CharacterAppearance` - selected tab -
dirty flags / last applied hash

## 16.2 Binding Rules

-   UI never touches renderers directly.
-   UI updates `CreatorState.Appearance`.
-   A debounced controller triggers `Builder.ApplyPartial(changes)`.

## 16.3 Panels

-   FacePanel: grid of face presets (icons)
-   BodyPanel: sliders (breast/butt/leg length)
-   HairPanel: hair list + color pickers
-   OutfitPanel: slot lists
-   FinalizePanel: save/load + rotate preview

------------------------------------------------------------------------

# 17. Save/Load

## 17.1 JSON Format

Save `CharacterAppearance` as JSON. Keep stable field names
(versioning-friendly).

## 17.2 Versioning

Add: - `appearanceVersion` - migration strategy for future fields
(optional but recommended)

------------------------------------------------------------------------

# 18. Performance & Streaming (PRO)

-   Use Addressables for large content (hair/outfits/face variants)
-   Cache loaded prefabs and meshes
-   Partial apply to avoid rebuilding everything on slider changes
-   MaterialPropertyBlock prevents material instancing explosion
-   Consider LOD Groups for characters outside creator scene

------------------------------------------------------------------------

# 19. Implementation Roadmap

## Phase 1 (MVP)

-   Catalogs + CharacterAppearance
-   Builder with Hair + 2 equipment slots
-   Hair runtime color with MPB
-   Body bone scaling (breast/butt/leg length)
-   Basic Creator UI + JSON save/load

## Phase 2 (Face System)

-   Face preset catalog
-   Mesh variant swapping implementation
-   Camera focus per tab

## Phase 3 (PRO)

-   Assembly graph + partial apply hashing
-   Body masking rules & tooling
-   Addressables streaming
-   Preset editor tools

------------------------------------------------------------------------

# 20. Claude Implementation Tasks (Suggested)

1.  Implement `CharacterAppearance` + `SerializableColor`
2.  Implement Catalog SOs and a `CatalogProvider`
3.  Implement `BoneMapCache` (name → transform)
4.  Implement `CharacterBuilder` with pipeline steps
5.  Implement `EquipmentBinder` (SkinnedMesh bone rebind)
6.  Implement `HairColorApplier` using MaterialPropertyBlock + shader
    adapter
7.  Implement `CreatorState` + panels + bindings + debounce
8.  Implement Save/Load JSON + basic migrations
9.  Implement Face variant swapping (mesh swap + bone rebind)
10. Add camera focus logic (Cinemachine targets)

------------------------------------------------------------------------

END OF SPECIFICATION
