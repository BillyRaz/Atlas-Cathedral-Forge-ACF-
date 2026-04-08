# ACF Next Update Roadmap

## Direction

The current ACF core is already strong enough for production blockout work.

Current ACF core covers:

- floor layout
- corridor flow
- wall generation
- roof generation
- room connection logic
- blockout replacement

The next update should not redefine the current core. It should extend ACF from a room-and-corridor blockout tool into a structural building tool.

That means the next phase of ACF should focus on:

- inside walls
- outside walls
- wall separation logic
- proper stair generation
- tower and stair-tower support
- stronger structural replacement and edit workflow

The goal is to move from:

- disconnected room boxes

to:

- real architectural structures
- connected interior spaces
- vertical traversal
- shell and partition logic
- building-scale blockout systems

## ACF Core V2

### Pillar 1: Wall Typing

Wall generation should stop treating every wall as the same type of object.

ACF should support structural wall distinction such as:

- `OuterWall`
- `InnerWall`
- `ConnectorWall`

These can be implemented either as:

- full categories
- subtypes stored in metadata

The important part is that the system knows the difference.

#### Outer Walls

Outer walls define the visible building shell.

They may later support:

- thicker structure
- exterior-facing replacement meshes
- facade logic
- windows
- supports or buttresses

#### Inner Walls

Inner walls define interior separation between spaces.

They may later support:

- thinner structure
- doorway cuts
- corridor transitions
- room partition behavior

#### Connector Walls

Connector walls are useful for seams between spaces, towers, and transitional structural pieces.

## Pillar 2: Interior Separation

ACF should support room-to-room and room-to-corridor wall separation cleanly.

Instead of only generating perimeter shells around floor edges, ACF should also understand:

- interior room boundary walls
- partition lines
- doorway cut points
- shared wall splitting

This is critical for building:

- chapels
- libraries
- tower rooms
- stair halls
- crypt wings
- side chambers

The next update should begin turning floor layouts into real internal architecture.

## Pillar 3: Structural Replace Workflow

The replace and edit workflow should become more structural-aware.

Current ACF already supports replacement, but the next version should understand:

- wall segments
- structural groups
- room modules
- stair modules
- tower modules
- outer vs inner placement intent

### Target Behavior

The Final / Replace stage should be able to replace by:

- selected objects
- category
- subtype
- generated structure type

Examples:

- replace all `OuterWall`
- replace all `InnerWall`
- replace all `GeneratedStairs`
- replace all `TowerWall`
- replace all `RoofCap`

This would make blockout-to-final conversion much more controlled.

## Pillar 4: Stair Generator

ACF should gain a proper stair setup so vertical construction stops depending on manual stair building.

### Minimum Good Stair System

The first stair system does not need to solve every stair shape.

The first version should support:

- straight stairs
- start floor
- target floor
- height difference
- stair width
- step count or automatic calculation
- top and bottom alignment
- optional landing

This alone would already save major time during building construction.

### Later Stair Expansion

After the first stable stair version, ACF can expand into:

- L-shaped stairs
- U-shaped stairs
- multi-landing stairs
- enclosed stair towers

## Pillar 5: Tower Support

ACF should support tower and stair-tower workflows as proper structural systems.

This means combining:

- floor logic
- wall logic
- stair logic
- shell generation

into a unified vertical structure workflow.

The goal is not just rooms and corridors anymore.

The goal becomes:

- towers
- stair towers
- connected vertical routes
- structural shells
- upper-floor expansion

## Identity Shift

This next update is important because it changes ACF’s identity.

### Before

ACF primarily helps with:

- room layout
- corridor layout
- shell walls
- blockout editing

### Next

ACF should become a system for:

- architectural blockout
- interior separation
- structural shell building
- vertical building logic
- tower construction
- stair generation

That direction fits the name **Atlas Cathedral Forge** much more strongly.

## Recommended Next-Core Scope

To keep the next update focused, ACF Core V2 should be structured as:

### 1. Wall Typing

Add support for:

- outer walls
- inner walls
- connector wall segments

### 2. Interior Separation

Generate room-to-room separation walls cleanly.

### 3. Structural Replace Workflow

Improve replace and edit so structural pieces can be handled by type.

### 4. Stair Generator

Add real stair generation with height-aware blockout creation.

### 5. Tower Support

Allow stair towers and vertical shell building using floor, wall, and stair logic.

## Suggested Version Label

This next phase can be treated as:

**ACF Core V2 — Structural Build Phase**

## Notes

This roadmap is intentionally focused on core structural growth, not polish-only improvements.

The current ACF build is considered stable enough for active use.

The next update should build on that stable base instead of replacing it.

## Author Direction

Roadmap structured from the current ACF production workflow under the RAZ build direction.
