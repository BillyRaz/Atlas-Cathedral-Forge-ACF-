# Atlas Cathedral Forge (ACF)

Atlas Cathedral Forge (ACF) is a Unity editor toolset for fast level blockout, floor-plan snapping, wall and roof generation, object categorization, and final prefab replacement.

The latest update also adds a portable door/key interaction layer so the package can be reused across projects without depending on a specific player controller.

This repository currently contains the completed core ACF workflow:

- `Scan`
- `Blockout`
- `Floor`
- `Walls`
- `Edit`
- `Final`
- `Diagnose`

## Core Workflow

### 1. Scan

Use the ACF window to scan the active scene and categorize objects into:

- `Floor`
- `Wall`
- `Roof`
- `Prop`
- `MovableProp`
- `Door`
- `Key`
- `Landmark`
- `Ignore`

The scan system uses:

- existing `ACFObjectData`
- Unity tags
- object names
- fallback heuristics

It also applies a temporary grayscale grid preview to source scene geometry for blockout-style readability.

### 2. Blockout

Create blockout prefabs from scanned scene content and spawn them back into the scene for fast layout iteration.

Features:

- create / refresh blockout prefabs
- spawn blockout prefabs by category
- select scene blockouts by category
- link keys to doors
- assign key data to detected key objects
- keep generated blockouts on a shared opaque material

### 3. Floor

Build and manage floor layouts before wall generation.

Features:

- choose a `Main Floor`
- choose a `Target Floor`
- snap floor edges together
- improved edge alignment when snapping floors with different dimensions
- mark selected floors as `Room`
- mark selected floors as `Corridor`
- create `Mask Floor` objects from roof objects for upper-floor work

### 4. Walls

Generate walls, connector doors, and roofs from the floor layout.

Features:

- per-side door counts
- shared-edge corridor and room connection handling
- connected-floor seam detection
- bulk door selection and lock-state tools
- optional roof generation
- optional mask-floor generation instead of roof generation

### 5. Edit

Real-time editing for selected objects using each object's own pivot.

Features:

- live position offset
- live rotation offset
- live scale multiplier
- multi-object editing
- individual-pivot behavior

### 6. Final

Replace blockout or categorized objects with final prefabs while preserving transforms.

### 7. Diagnose

Analyze the scene and generated content.

Features:

- object statistics
- renderer and collider counts
- memory estimate
- deep name analysis
- generated blockout counts
- floor subtype reporting

## Floor Subtypes

ACF currently uses the standard `Floor` category with additional floor subtypes stored in `ACFObjectData.customCategory`.

Supported subtypes:

- `CorridorFloor`
- `MaskFloor`

These subtypes are used to improve wall generation and multi-floor workflows without breaking the core category structure.

## Generated Object Controls

ACF includes shared generated-object visibility tools across tabs:

- hide/show generated objects
- hide/show only generated doors
- hide/show only generated roofs

These controls affect generated blockout objects only, not hand-placed scene objects.

## Portable Runtime

The package now includes reusable runtime helpers for shared projects:

- `ACFDoorData` with hinge pivots, push detection, obstacle checks, and lock/key support
- `ACFKeyData` auto-configuration from object names
- `ACFKeyPickup` with prompt UI and generic collector detection
- `ACFPlayerKeyRing` for project-agnostic key ownership
- prompt and debug helpers for door/key testing

These runtime pieces are designed to work without a hard dependency on a game-specific player controller. A project can integrate them with tagged players, `CharacterController`, `Rigidbody`, or a custom setup that carries an `ACFPlayerKeyRing`.

## Main Scripts

- `Assets/Atlas Cathedral Forge (ACF)/Script/Editor/ACFWindow.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Editor/ACFToolbar.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Editor/ACFObjectDataEditor.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Editor/ACFDoorDataEditor.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Editor/ACFKeyDataEditor.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Editor/ACFTagSetup.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Runtime/ACFObjectData.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Runtime/ACFCategoryUtility.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Runtime/ACFKeyData.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Runtime/ACFDoorData.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Runtime/ACFKeyPickup.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Runtime/ACFPlayerKeyRing.cs`

## Current Status

This version is considered the completed core ACF workflow. Future work can focus on improvements, refinements, and additional tooling rather than missing foundation features.

## Next Roadmaps

- Core structural roadmap: [NEXT_UPDATE_ROADMAP.md](D:/Unity%20Project/Atlas-Cathedral-Forge-ACF-/NEXT_UPDATE_ROADMAP.md)
- Optional map extension roadmap: [MAP_EXTENSION_ROADMAP.md](D:/Unity%20Project/Atlas-Cathedral-Forge-ACF-/MAP_EXTENSION_ROADMAP.md)

## Author

RAZ
