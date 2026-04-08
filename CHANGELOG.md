# Changelog

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
