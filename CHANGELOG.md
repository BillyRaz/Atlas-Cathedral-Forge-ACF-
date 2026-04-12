# Changelog

## Portable Door And Editor Upgrade

### Added

- portable runtime door system with hinge pivots, push opening, obstacle checks, and lock/key support
- portable key pickup flow with prompt UI and project-agnostic key ring support
- reusable `ACFPlayerKeyRing`, door prompt UI, key prompt UI, and debug helper scripts
- dedicated inspectors for `ACFDoorData` and `ACFKeyData`
- generated opaque blockout material asset under `Assets/Atlas Cathedral Forge (ACF)/Generated`

### Improved

- `ACFWindow` scan flow now syncs detected categories back into `ACFObjectData`
- `ACFWindow` can assign key data to detected key objects and auto-configure their runtime setup
- floor edge snapping now aligns mixed-size floors more cleanly
- generated blockouts, walls, roofs, and doors keep a shared opaque material
- door generation now creates ACF-ready door placeholders with editable lock settings
- scene organization skips UI-related roots more safely
- `ACFObjectData` and its inspector now track scanned/assigned Unity tag and layer metadata

### Notes

- this update pulls in the tested non-player ACF improvements from the BabyMonster project while keeping the package reusable across future player implementations
- the runtime now avoids hard dependencies on a project-specific player controller
 
## ACF Core Release

### Added

- unified ACF editor workflow window with tabs for `Scan`, `Blockout`, `Floor`, `Walls`, `Edit`, `Final`, and `Diagnose`
- shared category utility for standardized categorization logic
- deep scene diagnosis with category, object, collider, and memory reporting
- blockout prefab generation and scene blockout spawning
- real-time multi-object editing using each object's own pivot
- floor snapping system with explicit `Main Floor` and `Target Floor`
- corridor floor subtype support
- mask floor subtype support
- automatic wall generation from floor layouts
- shared-edge corridor and room connection wall logic
- generated door placeholder support
- generated roof support
- mask-floor generation from roof workflow
- generated object visibility tools across tabs
- key-to-door link data support
- editor helpers for object data and tag setup

### Improved

- wall detection and auto-categorization by name
- handling of connected floors to avoid unwanted internal walls
- shared seam generation to prevent duplicate connector doors
- diagnose output to include generated wall, door, roof, corridor, and mask-floor data
- UI layout in narrow editor panels with boxed setup sections

### Fixed

- wall objects being categorized as movable props
- scan pollution from organizer helper objects
- zero-result scan regression after hierarchy organization
- duplicate shared-edge door generation
- oversized shared seam door openings between connected rooms
- roof-generation confusion between old mask floors and new roof objects
- roof and mask-floor conversion consistency when toggling generation mode

## Notes

This changelog covers the completed core build milestone of ACF. Future entries should track refinements and quality-of-life improvements separately from the core foundation work.
