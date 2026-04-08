# Atlas Cathedral Forge (ACF)

Atlas Cathedral Forge (ACF) is a Unity editor toolset for fast level blockout, floor-plan snapping, wall and roof generation, object categorization, and final prefab replacement.

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

### 3. Floor

Build and manage floor layouts before wall generation.

Features:

- choose a `Main Floor`
- choose a `Target Floor`
- snap floor edges together
- mark selected floors as `Room`
- mark selected floors as `Corridor`
- create `Mask Floor` objects from roof objects for upper-floor work

### 4. Walls

Generate walls, connector doors, and roofs from the floor layout.

Features:

- per-side door counts
- shared-edge corridor and room connection handling
- connected-floor seam detection
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

## Main Scripts

- `Assets/Atlas Cathedral Forge (ACF)/Script/Editor/ACFWindow.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Editor/ACFToolbar.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Editor/ACFObjectDataEditor.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Editor/ACFTagSetup.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Runtime/ACFObjectData.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Runtime/ACFCategoryUtility.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Runtime/ACFKeyData.cs`
- `Assets/Atlas Cathedral Forge (ACF)/Script/Runtime/ACFDoorData.cs`

## Current Status

This version is considered the completed core ACF workflow. Future work can focus on improvements, refinements, and additional tooling rather than missing foundation features.

## Next Roadmaps

- Core structural roadmap: [NEXT_UPDATE_ROADMAP.md](D:/Unity%20Project/Atlas-Cathedral-Forge-ACF-/NEXT_UPDATE_ROADMAP.md)
- Optional map extension roadmap: [MAP_EXTENSION_ROADMAP.md](D:/Unity%20Project/Atlas-Cathedral-Forge-ACF-/MAP_EXTENSION_ROADMAP.md)

## Author

RAZ
