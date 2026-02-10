# Wiesenwischer GameKit — AAA Studio Technical Design Document
**Character Platform (CC5 + HD Anatomy → Unity 2022 LTS HDRP)**  
Version: **8.0** (AAA Studio Tech Design Doc)  
Date: **2026-02-09**  
Owner: **Wiesenwischer GameKit / Character Platform**  
Audience: Tech Lead, Engine/Gameplay Engineers, Tech Art, Tools, AI coding agents (Claude)

---

## Executive Summary

We are building a **Character Platform** (not a single “character creator feature”) that supports:

- **Player Character Creation** (UI-driven customization)
- **NPC Generation / Population Diversity**
- **Modular equipment** (hair, armor, accessories)
- **AAA-ish rendering** using **CC5 Auto Setup** shaders (HDRP)
- **Deterministic reconstruction** via **Appearance DNA** (serialization + multiplayer readiness)
- **Scalability** through **Graphs** (Authoring vs Runtime) and a **dependency-based Assembly Graph**

Key decisions:
1. **AppearanceDNA** is the **single source of truth** (SSOT).
2. Use a **Preview Proxy** in Creator scenes (strict separation from gameplay characters).
3. Use **Dual Graphs**:
   - **Authoring Graph** (interactive, “expensive”, UI/presets/preview)
   - **Runtime Assembly Graph** (deterministic, minimal cost)
4. Build body variation from **neutral base per gender** + **HD Anatomy archetypes** converted into **BlendShapes** (in Blender).
5. Treat **Body Ratio** as a distinct layer (prefer **bone-driven** proportions).
6. Face customization uses a **layered hybrid**:
   - **Structural & feature morphs** (BlendShapes)
   - **Micro adjustments** (Face bones)
   - **Materials** (eyes/skin/makeup)
7. Add **DNA Space + Constraints + Variation Model** for robust presets, mixing, and randomization.

This document includes the **why**, **why not**, tradeoffs, failure modes, and implementation guidance.

---

# 1. Problem Statement

Character systems fail in production primarily due to **state fragmentation** and **combinatorial explosion**:

- UI maintains one state; mesh has another.
- Equipment changes break morphs, and vice versa.
- Savegames become coupled to engine indices (blendshape indices, material instances).
- Multiplayer sync becomes expensive because you replicate “results” instead of “causes”.
- Adding new assets forces refactors because the architecture did not anticipate variation and constraints.

We must solve:
- **Consistency**: One authoritative model of appearance.
- **Scalability**: Hundreds of assets + many parameters without spaghetti.
- **Determinism**: Same DNA reconstructs same character on any machine/session.
- **Performance**: Update only what changed; avoid per-character material duplication.
- **Authoring workflow**: Tech artists can add content without code changes.

---

# 2. Goals & Non-Goals

## 2.1 Goals
- **High fidelity** look leveraging CC5 shaders and HD anatomy.
- **Body customization** based on HD Anatomy archetypes (female/male sets).
- **Face customization**: presets + sliders for shape + selectable features (ears/noses/eyes).
- **Eyes**: shape + iris/pupil + color + eyelids.
- **Skin**: tone and aging (structural + material-based).
- **Equipment**: modular armor and hair with reliable skeleton binding.
- **Creator UX**: stable presets + offsets; undo-friendly; save/load.
- **Extensible**: tattoos, scars, makeup, race/ethnicity styling (if desired later), procedural NPCs.
- **Game-agnostic combat**: architecture must not assume tab targeting or action combat. (Combat affects animation/gear rules but not the appearance core.)

## 2.2 Non-Goals (for v1)
- Full procedural sculpting in-engine.
- Player-imported arbitrary meshes.
- Photogrammetry pipeline.
- Full runtime cloth simulation authoring (we may support physics components but not authoring).

---

# 3. Terminology (Canonical)

- **AppearanceDNA**: The authoritative data describing character appearance in semantic parameters.
- **DNA Space**: A parameter space where each character is a point/vector.
- **Preset**: A named DNA point (or partial DNA) used as a starting coordinate.
- **Offset**: User adjustments applied on top of a preset.
- **Morph Channel**: A semantic parameter (e.g., Body.Weight) that influences one or more drivers.
- **Driver**: A mechanism that applies DNA to the character instance:
  - BlendShapeDriver (morph weights)
  - BoneDriver (bone scale/pose)
  - MaterialDriver (MPB parameter changes)
  - EquipmentDriver (spawn + bind)
- **Preview Proxy**: A creator-only visual character instance used for editing.
- **Assembly Graph**: Dependency graph that updates only affected parts of the character when DNA changes.

---

# 4. Top-Level Architecture

## 4.1 Systems Overview (Conceptual)
```
Creator UI  ─┐
             ├→ AppearanceService → AppearanceDNA → ConstraintService → (Authoring Graph) → Preview Proxy
Presets/Var  ┘

Saved DNA / Network DNA → (Runtime Assembly Graph) → Gameplay Character Instance
```

**Design intent**:
- Creator and Gameplay never share mutable state.
- DNA is the bridge between authoring and runtime.

## 4.2 Why Dual Graph?
### Problem
A single graph becomes a “god system” that must handle:
- UI responsiveness, presets, undo
- deterministic gameplay spawns
- performance budgets
- different lighting/cameras

### Decision
Split into:
- **Authoring Graph** (fast iteration, can be heavier)
- **Runtime Graph** (minimal, deterministic)

### Tradeoffs
- + Cleaner boundaries, fewer runtime dependencies
- + Gameplay safety (no Creator/UI baggage)
- - Two graphs to maintain (mitigated by sharing core mapping + nodes)

---

# 5. Key Decision: AppearanceDNA as Single Source of Truth

## 5.1 Problem
Without SSOT:
- UI slider state diverges from mesh state
- Equipment changes invalidate earlier morph calculations
- Savegames store engine-specific artifacts (indices, material instances)
- Multiplayer replicates large derived state

## 5.2 Decision
**All appearance is defined only by AppearanceDNA**.
Everything else is derived and re-derivable.

## 5.3 Alternatives Considered
1. **Store blendshape weights directly**  
   Rejected: couples to import order/index changes; breaks across asset updates.
2. **Store prefab variants**  
   Rejected: combinatorial explosion; difficult to integrate new gear.
3. **Store “final meshes” per character**  
   Rejected: memory, streaming, and pipeline complexity.

## 5.4 Tradeoffs
- + Deterministic reconstruction
- + Easy save/load and multiplayer replication
- + Engine-agnostic semantics
- - Requires a mapping layer (DNA → engine parameters)

---

# 6. AppearanceDNA (Domain Model)

## 6.1 DNA Structure (v1)
We keep DNA semantic and stable. Suggested structure:

```yaml
AppearanceDNA:
  version: 1
  gender: Female|Male
  body:
    weight: 0..1
    muscle: 0..1
    fitness: 0..1 (female)
    ageBody: 0..1 (male uses elder archetype influence; optional separate from face)
    archetypeBlend: [..] (vector; see below)
    ratio:
      height: 0..1
      legLength: 0..1
      shoulderWidth: 0..1
  face:
    presetId: string (optional)
    archetypeBlend: [..] (vector)
    nose:
      width: 0..1
      length: 0..1
      bridge: 0..1
      tip: 0..1
      typeId: string (optional, if “type as one-hot morph group”)
    eyes:
      size: 0..1
      spacing: 0..1
      tilt: 0..1
      depth: 0..1
      eyelid: 0..1
      typeId: string (optional)
    ears:
      size: 0..1
      angle: 0..1
      typeId: string (optional)
    jaw:
      width: 0..1
      chin: 0..1
      cheeks: 0..1
  skin:
    melanin: 0..1
    undertone: -1..1 (optional)
    redness: 0..1 (optional)
    age: 0..1
    freckles: 0..1
    makeupIds: [..] (optional)
  eyes:
    irisColor: "#RRGGBB"
    irisPatternId: string
    pupilSize: 0..1
    wetness: 0..1 (optional)
  equipment:
    hairId: string
    eyebrowId: string (optional)
    beardId: string (optional)
    outfitSlots:
      head: string
      chest: string
      hands: string
      legs: string
      feet: string
      back: string
    accessories: [..]
```

### Notes
- `version` allows migrations.
- `archetypeBlend` supports both “pick one” and “mix many” workflows.

## 6.2 DNA Vectors (ArchetypeBlend)
We treat HD Anatomy shapes as **basis targets**.  
We store the **semantic coordinate** rather than direct weights whenever possible.

Two options:
- **Option A (simple)**: store explicit weights per archetype target.
- **Option B (preferred)**: store weights only for master axes (weight/muscle/fitness) and compute archetype weights via Morph Graph.

In v1, we can store both:
- `body.weight` etc as canonical inputs
- `body.archetypeBlend` as derived cache (optional), not authoritative

Recommendation: **Do not persist derived weights** unless needed for backwards compatibility.

---

# 7. DNA Space

## 7.1 Concept
DNA is a vector. Presets are points. Sliders move the point.

## 7.2 Why DNA Space?
- Preset mixing becomes linear algebra (lerp).
- Random generation is “sample space then constrain”.
- Constraint enforcement becomes systematic.

## 7.3 Preset Mixing
Given two presets A and B (DNA vectors), mixing is:
`DNA = A*(1-t) + B*t` applied per channel with channel-specific rules.

Channel-specific mixing rules:
- Continuous floats: linear interpolation
- Discrete IDs (hairId): choose A or B based on t or weighted probability
- One-hot “type” groups: blend then re-normalize or choose

---

# 8. DNA Constraints (Realism + Technical Safety)

## 8.1 Why Constraints?
Unconstrained spaces yield broken/ugly characters and technical issues:
- extreme ratios break armor fitting and IK
- conflicting face feature extremes look alien
- heavy muscle + heavy fat + thin jaw = implausible silhouette

## 8.2 Types
1. **Range**: clamp per channel
2. **Dependency**: enforce relationships (soft or hard)
3. **Region**: block invalid combinations (rare; prefer soft)
4. **Soft correction**: subtle automatic compensation (AAA-style)

## 8.3 Example Constraints (recommended)
- If `body.weight` > 0.8 then reduce max `face.jaw.width` and increase `face.cheeks` baseline.
- If `skin.age` > 0.7 then reduce max `eyes.size` slightly and increase wrinkle intensity.
- If `ratio.legLength` increases, auto adjust `ratio.height` to maintain plausible proportions (optional).

## 8.4 Constraint Execution Order
1. Apply direct user change to DNA
2. Apply range clamps
3. Apply dependency constraints (soft)
4. Apply region constraints (rare)
5. Emit final corrected DNA

**Important**: Constraints are applied **before** marking graph dirty to prevent “preview flicker” and unstable oscillations.

---

# 9. Body System Design (HD Anatomy)

## 9.1 Source Inputs
HD Anatomy provides these archetype targets:
- Female: Athletic, Fit, Heavy, Plump, Slim
- Male: Athletic, Bodybuilder, Drudge, Elder, Heavy, Skinny, Slim
Each includes a “Body Ratio” component (proportion) which we handle separately.

## 9.2 Decision: Neutral Base per Gender
We use:
- Female_Base (neutral)
- Male_Base (neutral)

**Why**:
- stable deltas for BlendShapes
- consistent equipment fit
- easier LOD and baking

**Failure mode if not**:
- mixing between presets produces unexpected artifacts (nonlinear shape mixing)
- equipment clipping increases dramatically

## 9.3 How We Get BlendShapes
CC5 often does not export all body morphs as blendshapes.
We therefore use **mesh-target deltas** created via Blender shape keys.

Pipeline:
1. Export Base (neutral)
2. Export each archetype (same character, same topology)
3. In Blender: Base mesh gets shape keys from each archetype
4. Export one FBX containing all shape keys

## 9.4 Body Ratio Layer (Separate)
**Decision**: Treat “Body Ratio” as **bone-driven proportions** (primary).
Reasons:
- avoids geometry distortions that break armor
- consistent for IK and animation
- easier to constrain

Implementation:
- `ratio.height` affects overall scale (careful with gameplay)
- `ratio.legLength` scales upper/lower leg bones (small range)
- `ratio.shoulderWidth` scales clavicle/upper torso bones (small range)

**Notes**:
- Keep ranges conservative (e.g., 0.95–1.05) and rely on morphs for most variation.
- Use Animation Rigging/Foot IK if leg length changes affect foot grounding.

---

# 10. Face System Design (AAA Hybrid)

## 10.1 Face Requirements
- 12 face shapes (archetype presets)
- sliders for face shape (structural)
- selection for nose and ears “types” (and additional sliders)
- eye system: shapes + iris/pupil + eyelids + color
- skin tone and aging

## 10.2 Why Face ≠ Body
Face is visually sensitive:
- small changes matter more
- expression system interacts with shape system
- too many blendshapes can explode memory and authoring complexity

## 10.3 Layered Face Architecture
1. **Face Archetypes** (12): “starting points” (BlendShapes)
2. **Structural Morphs**: face proportions (BlendShapes)
3. **Feature Morphs**: nose/eyes/ears/jaw (BlendShapes)
4. **Micro adjustments**: eyelid openness, eye direction (Face bones)
5. **Materials**: skin/eyes/makeup/aging overlays

## 10.4 Decision: Bones for Micro, BlendShapes for Anatomy
### Why bones?
- cheap
- stable with expressions
- supports “live” behaviors (blink/look)

### Why blendshapes?
- true anatomical change (nose width, jaw width) is better as geometry deltas
- avoids odd bone scaling artifacts

---

# 11. Skin Tone & Aging

## 11.1 Use CC5 Skin Shader (HDRP)
CC5 Auto Setup provides a high quality shader stack for skin, eyes, hair.
We leverage it rather than writing a new shader.

### Runtime parameterization
We must not duplicate materials per character.
Use:
- `MaterialPropertyBlock` (MPB) for per-character parameters

Typical parameters (names may vary; implement as a mapping table):
- Skin tint / melanin proxy
- SSS intensity/profile selection
- wrinkle intensity / mask strength
- roughness modifier
- detail normal strength

## 11.2 Aging is Two-Part (AAA)
Aging is not only wrinkles:
1. **Structural aging** (BlendShapes): eye bags, sagging, folds
2. **Material aging**: wrinkle map strength, roughness, subtle color shifts

### Decision
Age slider drives both, via Morph Graph + Material drivers.

**Why**:
- structural only looks “rubbery”
- texture only looks “painted on”

---

# 12. Eye System (Detailed)

Eyes are a major “AAA believability” factor.

## 12.1 Components
- **Eye shape** (head morphs): size, spacing, tilt, depth
- **Eyelids**: morph or bone micro (blink base openness)
- **Iris color**: material parameter
- **Pupil size**: shader param or iris material param
- **Wetness/Specular**: eye shader parameters (optional)

## 12.2 Recommended Implementation
- Eye color/pupil/wetness via MPB
- Eye shape via head morphs
- Eyelid openness default via face bones (micro) + eyelid blendshape for larger structural changes

---

# 13. Equipment System (Modular Armor & Hair)

## 13.1 Requirements
- Equip/unequip hair and armor at runtime
- no “all-in-one exported character” dependency
- reliable binding to CC5 skeleton
- clipping prevention

## 13.2 Decision: Separate Prefabs + Runtime Bone Rebind
Each gear piece is its own prefab with one or more SkinnedMeshRenderers.

At runtime:
1. Instantiate prefab
2. Build BoneMap from Character rig
3. Rebind each SMR:
   - assign `rootBone`
   - rebuild `bones[]` array by name lookup
4. Parent under slot transform

## 13.3 Clipping Strategy: Hide Body Regions
AAA approach often hides body under armor.
We use either:
- **Hide blendshapes** on body (recommended for v1)
- or material mask (more flexible but more shader work)

Each equipment definition includes:
- `BodyHideMask`: list of body regions/blendshapes to set to 100

## 13.4 Failure Modes & Guardrails
- If bind poses mismatch: deformation artifacts
- If bone names differ: missing bind → invisible mesh or wrong pose
- If equipment contains its own armature/root: can become “Prefab instance child cannot be moved” / invisible issues

Mitigation:
- authoring rule: equipment exports must not include extra root transforms beyond mesh container
- runtime step can strip/disable equipment root armature objects if present
- validate bone coverage at import time (editor utility)

---

# 14. Creator vs Gameplay Characters (Preview Proxy Pattern)

## 14.1 Decision
Creator uses PreviewProxy (visual-only). Gameplay uses separate runtime character prefab.

**Why**:
- avoids gameplay animator overriding creator face bones
- avoids network components affecting preview
- allows dedicated lighting/camera composition for AAA look
- reduces coupling and refactor risk

## 14.2 Workflow
- Creator edits DNA → preview updates immediately
- Save button serializes DNA JSON
- Gameplay loads DNA JSON → spawns runtime character → applies DNA via runtime assembly graph

---

# 15. Scene Architecture (Creator Scene)

## 15.1 Structure
```
CharacterCreatorScene
  Systems
  Environment (neutral backdrop)
  Lighting (HDRP volume, key/fill/rim, reflection probe)
  Cameras (Cinemachine orbit, focus points)
  PreviewProxy (character instance)
  UI (tabs + panels)
```

## 15.2 UI Rules
UI never touches:
- blendshape weights
- materials
- equipment GameObjects
directly.

UI modifies only:
- AppearanceDNA via AppearanceService

---

# 16. Assembly Pipeline vs Assembly Graph

## 16.1 Why Graph?
Pipeline (linear) rebuilds too much on small changes.
Graph allows minimal recomputation.

## 16.2 Assembly Graph Nodes (Recommended)
- BaseCharacterNode (load / ensure refs)
- BodyMorphNode
- FaceMorphNode
- BodyRatioNode
- EquipmentNode
- HairNode
- MaterialNode
- FinalizeNode (bounds/LOD refresh)

## 16.3 Node Contract
Each node implements:

- Inputs (DNA fields it depends on)
- Dependencies (nodes that must run before it)
- Apply(ctx, dna)

Dirty marking:
- When a DNA subset changes, only nodes impacted become dirty.

Example:
- EyeColor change → MaterialNode dirty only.
- Weight change → BodyMorphNode + EquipmentNode (for hide masks) + FinalizeNode

---

# 17. Morph Graph (Inside Nodes)

Morph Graph is not the Assembly Graph.
Morph Graph is the internal logic mapping DNA channels to drivers.

Example:
`Body.Weight` influences:
- `F_Slim`, `F_Plump`, `F_Heavy` with a curve and normalization
- auto compensation of certain areas

We implement Morph Graph as:
- a set of mapping rules (curves) + optional dependency functions
- driven by DNA inputs

This is where you encode “AAA soft coupling”:
- weight affects breast volume slightly
- age affects cheek sag + wrinkle intensity

---

# 18. Mapping Layer (Critical Abstraction)

## 18.1 Why
Never embed engine indices in DNA or UI.
Assets evolve; indices change; names change.

## 18.2 Mapping Tables
We define:
- `BlendShapeMap`: semantic key → (rendererId, blendshapeName or index)
- `BoneMap`: semantic key → bone transform name(s)
- `MaterialParamMap`: semantic key → shader property name

Example keys:
- `Body.Female.Athletic`
- `Face.Nose.Width`
- `Skin.Melanin`
- `Eyes.IrisColor`

The mapping layer is loaded from ScriptableObjects in Content package.

---

# 19. Performance & Budgets (HDRP + Skinned Mesh)

## 19.1 Material Strategy
- Always use MPB where possible.
- Avoid instancing a new Material per character.
- Keep shader variant count controlled.

## 19.2 BlendShape Updates
- Batch calls per frame.
- Use “dirty blendshape list” to only set weights that changed.
- Consider throttling during slider drag (optional): update at 30Hz while dragging, 60Hz on release.

## 19.3 LOD
- LODGroup per character
- optional separate LOD for hair and accessories
- for creator preview: allow forcing highest LOD

---

# 20. Serialization & Versioning

## 20.1 Save Format
Serialize only DNA:
- JSON (human readable, easy debugging)
- include `version`

## 20.2 Migration Strategy
On load:
- if DNA version < current: migrate (fill missing fields with defaults)
- keep mapping tables separate; do not “bake” indices into DNA

---

# 21. Modular Unity Packages (GameKit)

Recommended packages:
- `GameKit.Character.Core`  
  DNA model, constraints, interfaces, shared utilities
- `GameKit.Character.Content`  
  mapping tables, presets, catalogs, addressables groups
- `GameKit.Character.Creator`  
  UI, authoring graph, preview proxy scene tooling
- `GameKit.Character.Runtime`  
  runtime assembly graph, character applier components

Rules:
- Runtime must not depend on Creator.
- Creator can depend on Runtime (shared node implementations), but must avoid importing gameplay scripts.

---

# 22. Implementation Plan (Phased)

## Phase 1 (MVP)
- Female_Base + Body archetype blendshapes
- Basic DNA + constraints (range)
- Preview Proxy scene + UI sliders (Weight/Fitness)
- Equipment: hair + one armor slot
- Save/Load DNA JSON

## Phase 2
- Male pipeline
- Face archetypes + key feature sliders (Nose/Eyes/Jaw)
- Skin melanin + age affecting materials
- Assembly Graph dirty evaluation

## Phase 3
- Variation Model (NPC generation)
- More constraints (dependency/soft)
- LOD integration + performance tuning
- Content authoring tools (validator)

---

# 23. Open Risks & Mitigations

- **CC5 export variability**: shader params differ by version → use mapping tables and validation scripts.
- **Blendshape name drift**: rename breaks mapping → enforce naming conventions; build importer checks.
- **Equipment skeleton mismatches**: require strict authoring guidelines and import-time validation.
- **Performance with many characters**: add throttling and LOD; limit blendshape complexity in gameplay.

---

# 24. Appendix A — HD Anatomy Archetype List

Female:
- Athletic
- Fit
- Heavy
- Plump
- Slim

Male:
- Athletic
- Bodybuilder
- Drudge
- Elder
- Heavy
- Skinny
- Slim

Each archetype export must be derived from the same neutral base character per gender (topology must match).

---

# 25. Appendix B — Recommended Initial Slider Set (v1)

Body:
- Weight
- Fitness (female) / Muscle (male)
- Age (face/skin), later split into bodyAge

Face:
- Face preset select (12)
- Nose: width, length
- Eyes: size, tilt, iris color, pupil size
- Jaw: width
- Ears: size + type

Skin:
- Melanin (tone)
- Age (wrinkles + sag)

---

# 26. Appendix C — Creator UX Rules

- Preset selection sets base DNA coordinate.
- Sliders apply offsets on top of preset.
- “Reset section” resets offsets only (does not change preset).
- Undo/redo stores DNA snapshots.
- Live preview uses throttled updates during drag if needed.

---

END OF DOCUMENT
