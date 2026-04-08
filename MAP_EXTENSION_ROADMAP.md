# ACF Map Extension Roadmap

## Purpose

This roadmap covers a future optional extension for ACF that generates a clean runtime map and menu map from ACF layout data.

This is not part of the current ACF core.

The map system should remain a separate extension layer so the main blockout and building workflow stays focused and lightweight.

## Why It Fits ACF

ACF already owns the correct source data:

- floor data
- prop data
- door data
- key data
- category metadata
- generated layout structure

That means a map system would be a natural extension of ACF rather than an unrelated side feature.

The intended architecture is:

- ACF builds the level
- ACF understands the level
- the map extension reads ACF data
- runtime map state updates from ACF-linked objects

## Extension Position

This system should be developed as a separate module, not mixed into the current ACF core.

Suggested module structure:

- `ACFMapDefinition`
- `ACFMapRuntime`
- `ACFMapIcon`
- `ACFMapRoom`
- `ACFMapRenderer`
- `ACFMapSpritePalette`

Benefits:

- ACF core stays clean
- the map system stays optional
- projects can disable it for minimal-UI builds
- the runtime map can evolve independently from the editor blockout system

## Core Design

### 1. Floors As The Base Map Layer

Floor data should define the main readable map layout.

Each floor piece can provide:

- bounds
- room type
- floor subtype
- floor ID
- discovered or undiscovered state
- sprite override

The map should be built from authored floor and room data, not from a screenshot-based approach.

### 2. Props And Doors As Markers

Props should only appear on the map when they are gameplay-important.

Examples:

- save point
- merchant
- locked door
- key pickup
- boss door
- stairs
- ladder
- puzzle object

This keeps the map readable and avoids icon noise.

### 3. Key Data Drives Door State

Because ACF already links keys and doors, the map extension can display meaningful runtime states.

Useful door states:

- locked
- unlocked
- opened
- cleared

This supports a strong survival-horror or exploration-style map flow.

### 4. Swappable Sprite Override

The map system should not hardcode a single visual style.

Each room or map object should support:

- default sprite
- override sprite
- icon override
- palette or theme override

This allows different visual themes without changing the runtime logic.

Possible themes later:

- cathedral
- crypt
- mechanical lab
- holy faction

## UI Philosophy

The map should exist as a tool, not as visual noise.

This fits a minimal UI game direction much better than a constant HUD-heavy design.

### During Gameplay

Keep the screen mostly clean.

Only show critical UI when needed:

- health if needed
- boss bar if needed
- interaction prompts if needed
- important status or damage feedback

### In Menus

Open a clean map and inventory interface through a simple one-click menu flow.

Examples:

- map
- inventory
- notes
- keys
- objective info

This supports a moody and atmospheric presentation without clutter.

## Recommended Runtime Layering

### Layer 1: Data Layer

Pure ACF-driven data:

- floors
- room bounds
- doors
- keys
- props
- stairs
- connections
- category or subtype states

### Layer 2: Runtime State Layer

Tracks live progression:

- discovered rooms
- unlocked doors
- collected key items
- cleared rooms
- solved interactions
- player location

### Layer 3: UI Render Layer

Draws the final output to:

- canvas map
- pause or menu map
- optional minimap

This separation will keep the extension maintainable and flexible.

## RE-Style Tracking Targets

### Room States

- hidden
- discovered
- uncleared
- cleared

### Door States

- unknown
- visible
- locked
- unlocked
- opened

### Item And Prop States

- available
- collected
- solved
- inactive

### Special Markers

- save room
- merchant
- stairs
- boss room
- puzzle room

## Best Development Order

This extension should not be built before the main ACF level-building workflow is fully proven in production use.

Recommended order:

1. finish using current ACF core in a real playable layout
2. confirm floor, corridor, wall, roof, and stair-direction needs
3. then build the map extension on top of stable ACF data

That keeps the map system grounded in real usage instead of speculation.

## Why The Current Layout Is A Good Test Case

The current cathedral-style layout is a strong future test bed for this system because it includes:

- a central hub
- side wings
- corridor transitions
- vertical progression
- locked gates
- stair transitions
- boss destination flow
- optional risk and reward branches

Possible future tagged spaces:

- main hall nave as hub
- west wing catacombs as side objective or puzzle branch
- east wing library as side exploration branch
- save chapel as safe room
- lower crypt as progression funnel
- underground dungeon as risk and reward layer
- ritual chamber as boss arena

## Status

This extension is planned but intentionally deferred.

The current priority remains:

- finish levels
- validate ACF in active production
- avoid overbuilding side systems too early

## Author Direction

Map extension roadmap structured under the RAZ build direction as a future optional system layered on top of ACF core.
