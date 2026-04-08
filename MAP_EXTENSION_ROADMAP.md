# ACF Map Extension Roadmap

## ACF Map Extension - Optional Runtime UI Layer

### Overview

The ACF Map Extension is a future optional module built on top of ACF core data.

It should not be merged into the core blockout and structural workflow. Instead, it should sit above ACF as a clean runtime and UI layer that reads the level information ACF already produces.

This extension is meant to support:

- full-screen map screens
- optional minimap support
- room discovery
- door and key state tracking
- RE-style room status flow
- clean menu-first UI

The goal is to move from:

> "ACF builds the level"

to:

> "ACF also provides the data layer for readable, optional runtime navigation UI"

---

## Core Design Principles

- ACF core remains **editor-first**
- map systems remain **optional**
- runtime map reads **ACF-authored structure data**
- UI should be **clean, minimal, and intentional**

---

## Why This Fits ACF

ACF already owns the right source data:

- floor data
- prop data
- door data
- key data
- category metadata
- generated layout structure

That means the future map system would not be a random side feature.

It would be a natural extension layer with the following logic:

- ACF builds the level
- ACF understands the level
- the map extension reads ACF data
- runtime map state updates from ACF-linked objects

---

## Module Position

This should be implemented as a separate module, not mixed into current ACF core.

### Suggested Module Structure

- `ACFMapDefinition`
- `ACFMapRuntime`
- `ACFMapIcon`
- `ACFMapRoom`
- `ACFMapRenderer`
- `ACFMapSpritePalette`

### Benefits

- ACF core stays focused
- map logic remains optional
- minimal-UI builds can disable it completely
- runtime map systems can evolve without destabilizing core editor tools

---

## Feature Breakdown

---

### 1. Floor-Driven Map Layout

#### Goal

Use floor data as the main source for readable map structure.

#### Features

Each floor piece can provide:

- bounds
- room type
- floor subtype
- floor ID
- discovered state
- sprite override

#### Benefits

- map layout is authored, not guessed
- rooms and corridor shapes stay consistent with ACF blockout
- supports upper-floor and mask-floor workflows later

---

### 2. Prop And Door Marker System

#### Goal

Use gameplay-relevant objects as selective map markers instead of showing every object.

#### Features

Only important objects should appear:

- save point
- merchant
- locked door
- key pickup
- boss door
- stairs
- ladder
- puzzle object

#### Benefits

- keeps the map clean
- avoids icon clutter
- supports atmospheric game presentation

---

### 3. Key And Door Runtime States

#### Goal

Use ACF key and door data to drive meaningful map feedback.

#### Features

Doors can expose states like:

- locked
- unlocked
- opened
- cleared

This works naturally because ACF already links keys and doors.

#### Benefits

- supports survival-horror style navigation
- gives the player useful progression feedback
- makes ACF data more valuable at runtime

---

### 4. Swappable Sprite And Theme Overrides

#### Goal

Keep the map system style-flexible instead of hardcoding one visual look.

#### Features

Each room or map object should support:

- default sprite
- override sprite
- icon override
- palette override
- theme override

#### Example Themes

- cathedral
- crypt
- mechanical lab
- holy faction

#### Benefits

- one system supports multiple visual identities
- style can change without changing map logic
- future projects stay reusable

---

### 5. Minimal UI Philosophy

#### Goal

Keep map UI useful without cluttering gameplay presentation.

#### Runtime Direction

During gameplay, only show UI when truly necessary:

- health if needed
- boss bar if needed
- interaction prompts if needed
- key status or critical state feedback if needed

#### Menu Direction

The main map should open through a clean one-click menu flow such as:

- map
- inventory
- notes
- keys
- objective view

#### Benefits

- supports immersion
- avoids cheap-looking HUD noise
- fits moody atmospheric game design

---

### 6. Three-Layer Runtime Architecture

#### Goal

Keep the extension stable by separating source data, runtime state, and rendering.

#### Layer 1: Data Layer

Pure ACF-driven data:

- floors
- room bounds
- doors
- keys
- props
- stairs
- connections
- categories and subtypes

#### Layer 2: Runtime State Layer

Tracks progression:

- discovered rooms
- unlocked doors
- collected key items
- solved interactions
- cleared rooms
- player location

#### Layer 3: UI Render Layer

Draws the result to:

- canvas map
- pause/menu map
- optional minimap

#### Benefits

- easier debugging
- cleaner code ownership
- easier optional feature support

---

### 7. RE-Style Room Tracking

#### Goal

Support a readable room-status system similar to survival-horror map design.

#### Room States

- hidden
- discovered
- uncleared
- cleared

#### Door States

- unknown
- visible
- locked
- unlocked
- opened

#### Item And Prop States

- available
- collected
- solved
- inactive

#### Special Markers

- save room
- merchant
- stairs
- boss room
- puzzle room

#### Benefits

- clearer exploration flow
- stronger progression readability
- better support for key-and-door gameplay loops

---

## Best Development Order

This extension should not be built before ACF core is fully proven in active level production.

Recommended order:

1. finish building real content with current ACF core
2. validate floor, wall, corridor, and structural workflows in production
3. then build the map extension on top of stable data and metadata

This keeps the map system grounded in actual game needs instead of speculation.

---

## Why The Current Cathedral Layout Is A Strong Test Case

The current layout is already a strong fit for a future ACF map system because it contains:

- a central hub
- side wings
- corridor transitions
- vertical progression
- locked gates
- stair transitions
- boss destination flow
- optional risk and reward branches

### Example Future Map Tags

- main hall nave as hub
- west wing catacombs as side objective or puzzle branch
- east wing library as side exploration branch
- save chapel as safe room
- lower crypt as progression funnel
- underground dungeon as risk and reward layer
- ritual chamber as boss arena

This makes the current level a strong real-world test bed for the future map extension.

---

## Recommended Scope

To keep the first version realistic, the map extension should be built in this order:

1. floor-driven map layout
2. prop and door markers
3. key and door runtime state
4. menu map renderer
5. optional minimap support
6. sprite and theme override system

---

## Version Direction

Suggested label:

**ACF Map Extension V1 - Runtime Navigation Layer**

---

## Notes

This extension is intentionally deferred.

The current priority remains:

- finish levels
- validate ACF in active production use
- avoid overbuilding optional side systems too early

The map should be treated as a tool, not as visual noise.

## Author Direction

Map extension roadmap structured under the RAZ build direction as a separate optional layer built on top of ACF core.
