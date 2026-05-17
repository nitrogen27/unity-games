# WolfMini Agent Notes

Primary Unity project: use the repository root at `/Users/kirillionov/unity-games`.
Do not treat the nested `WolfE1M1TwoRoomsUnity` folder as the main project unless a later user explicitly asks for it.

## MASTER SPEC

WolfMini is a small Unity first-person Wolfenstein3D-style demo scene. It is not a full clone, not a full game, and not an attempt to recreate all original systems.

The target experience is a compact handmade E1M1-style environment:

- Handmade rooms and corridors inspired by the E1M1 feel.
- Upper/lower floor layout with the upper floor at `y = 0` and lower floor at `y = -3`.
- A visible stair opening connecting the two elevations.
- Doors and a simple HUD are in scope.
- Pixelated retro presentation is in scope.
- Grid cell size is `2` Unity units.
- Wall height is `2` Unity units.
- Player eye height is `0.9` Unity units.
- HUD target layout is `640x80`.
- Enemies, weapons, combat, AI, pickups-as-gameplay, scoring, progression, and full-game systems are out of scope unless a later user explicitly changes the spec.

## Implementation Rules

- Keep the project focused on a small playable demo scene, not a complete Wolfenstein3D clone.
- Preserve the authored scale constants: cell size `2`, wall height `2`, eye height `0.9`, upper floor `y = 0`, lower floor `y = -3`, HUD `640x80`.
- Prefer handmade room/blockout authoring for the scene shape. Do not replace the target with a full imported original map.
- Keep verticality limited and readable: the stair opening must clearly connect the upper and lower floor.
- Doors should support the demo-scene feel without expanding into broader game progression.
- Preserve the pixelated retro style when touching visuals, materials, textures, UI, or camera presentation.
- Do not add enemies, weapons, combat loops, full inventory, full level progression, or complete-game systems as part of WolfMini work.
- Keep changes tightly scoped to the requested area. Avoid unrelated refactors, asset churn, scene rewrites, or generated-file noise.
- Do not overwrite user or teammate work. Inspect existing files before editing and preserve unrelated changes.
- For documentation-only tasks, do not change gameplay code, scenes, assets, materials, textures, project settings, or scripts.

## PR Checklist

- The change stays within the WolfMini scope: a small Unity first-person demo scene, not a full clone.
- The MASTER SPEC values are preserved: cell size `2`, wall height `2`, eye height `0.9`, upper `y = 0`, lower `y = -3`, HUD `640x80`.
- The scene intent remains handmade E1M1-style rooms with upper/lower floor structure, stair opening, doors, HUD, and pixelated retro presentation.
- No enemies, weapons, combat, AI, full-game progression, or unrelated gameplay systems were added.
- No gameplay code, scenes, assets, or scripts were changed unless the task explicitly requested implementation work.
- Only files in the requested/owned scope were edited.
- Existing teammate/user changes were not reverted or overwritten.
- If implementation files were changed, relevant Unity/editor validation or tests were run and the results are noted in the PR.
- If this is documentation-only, the PR states that no runtime validation was required.
