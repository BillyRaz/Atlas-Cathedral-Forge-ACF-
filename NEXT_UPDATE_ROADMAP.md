# ACF Next Update Roadmap

## ACF Core V2 - Structural Build Phase

### Overview

The next phase of Atlas Cathedral Forge (ACF) evolves the system from a **room and corridor blockout tool** into a **full structural building workflow**.

This update focuses on enabling users to construct complete architectural spaces such as:

- multi-room interiors
- connected corridors
- vertical structures (stairs, towers)
- outer building shells
- interior partitions

The goal is to transition from:

> "placing rooms"

to:

> "building structures"

---

## Core Design Principles

- Floors define **space**
- Walls define **structure**
- Stairs define **vertical movement**
- Replace step defines **final architecture**

---

## Feature Breakdown

---

### 1. Wall Structure System

#### Goal

Introduce clear separation between **outer walls** and **inner walls**, replacing the current generic wall logic.

#### Features

- detect **exposed floor edges** and generate **Outer Walls**
- detect **adjacent floor boundaries** and generate **Inner Walls**
- handle corridor seams separately as **Connector Edges**
- respect door openings during generation

#### Wall Types

This can be implemented through subtype or metadata:

- `OuterWall`
- `InnerWall`
- `ConnectorWall` (future-ready)

#### Benefits

- enables real building shells
- supports interior architecture
- improves final mesh replacement accuracy

---

### 2. Interior Wall Generation

#### Goal

Support proper **room-to-room separation** inside structures.

#### Features

- generate walls between connected rooms
- respect corridor vs room transitions
- prevent duplicate or overlapping wall segments
- maintain clean doorway gaps

#### Benefits

- moves ACF beyond perimeter-only layouts
- enables realistic interior design
- supports complex room layouts such as library, chapel, and crypt wings

---

### 3. Structural Replace And Edit Workflow

#### Goal

Upgrade Replace and Edit systems to work with **structural types**, not just broad categories.

#### Features

Replace by:

- category
- subtype
- generated structure type

#### Example Replace Targets

- `OuterWall`
- `InnerWall`
- `GeneratedStairs`
- `TowerSegment`
- `RoofShell`

#### Improvements

- preserve transform (existing)
- optional collider assignment (existing)
- add structural grouping awareness (new)

#### Benefits

- faster finalization pass
- cleaner blockout-to-final pipeline
- less manual cleanup

---

### 4. Stair Generation System

#### Goal

Replace manual stair building with a **procedural stair generator**.

#### Core Features

Define:

- start floor
- target floor
- height difference
- direction

Auto-calculate:

- step count
- step height

Generate:

- straight stair blockout
- optional landing platform

#### Initial Scope (V1)

- straight stairs only
- single direction
- optional landing
- blockout prefab output

#### Future Expansion

Not part of this phase:

- L-shaped stairs
- spiral stairs
- curved stairs

#### Benefits

- removes repetitive manual work
- ensures consistent scale and alignment
- supports vertical level design naturally

---

### 5. Tower And Stair Tower Support

#### Goal

Enable construction of **vertical architectural structures** such as towers.

#### Features

- use floor footprint to define tower base
- generate outer walls
- support vertical stacking logic
- integrate stair system inside tower
- support multi-floor vertical connections

#### Example Use Cases

- bell towers
- stair towers
- vertical dungeon shafts
- lookout towers

#### Benefits

- expands ACF into full 3D structure design
- supports vertical gameplay routes
- aligns with cathedral-style architecture

---

### 6. Structural Metadata System

#### Goal

Track generated structural elements for better control, diagnosis, and replacement.

#### Suggested Component

```csharp
public class ACFGeneratedStructure : MonoBehaviour
{
    public string structureType;
    public string sourceId;
}
```

#### Suggested Data Fields

- `structureType`
- `sourceId`
- `parentStructureId`
- `generatedFromCategory`
- `generatedFromSubtype`
- `connectionRole`

#### Benefits

- better diagnose output
- better replace targeting
- better edit grouping
- future-safe structural tooling

---

## Identity Shift

### Before

ACF primarily supports:

- room layout
- corridor layout
- shell walls
- blockout editing

### After V2

ACF should support:

- architectural blockout
- interior wall logic
- structural shell building
- vertical building logic
- tower construction
- stair generation

This direction fits the name **Atlas Cathedral Forge** far more strongly.

---

## Recommended Scope For The Next Core Release

To keep the next phase focused, ACF Core V2 should be delivered in this order:

1. Wall typing
2. Interior wall generation
3. Structural replace and edit workflow
4. Stair generator
5. Tower and stair tower support
6. Structural metadata system

---

## Version Direction

Suggested label:

**ACF Core V2 - Structural Build Phase**

---

## Notes

This roadmap is focused on **core structural growth**, not polish-only improvements.

The current ACF build is already stable enough for active use and level production.

The next update should build on that stable base rather than replacing it.

## Author Direction

Roadmap structured under the RAZ build direction for the next major ACF core phase.
