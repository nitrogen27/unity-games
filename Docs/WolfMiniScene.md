# WolfMini Scene Spec

WolfMini is a small Unity first-person Wolfenstein3D-style demo scene. The goal is a compact vertical slice, not a full clone and not a complete game.

## Scene Target

- Build a handmade E1M1-style set of rooms and corridors.
- Include an upper floor and a lower floor.
- Use a stair opening as the clear connection between floors.
- Include doors and a simple HUD.
- Keep the look pixelated and retro.

## Scale And Layout Constants

- Grid cell size: `2` Unity units.
- Wall height: `2` Unity units.
- Player eye height: `0.9` Unity units.
- Upper floor elevation: `y = 0`.
- Lower floor elevation: `y = -3`.
- HUD target layout: `640x80`.

## Explicit Non-Goals

- No enemies.
- No weapons.
- No combat loop or AI.
- No full inventory, score, progression, or complete-game systems.
- No attempt to recreate a full Wolfenstein3D level set or clone the original game.

## Working Boundaries

Future work should keep the demo small, readable, and handmade. Scene or code changes should preserve the constants above and avoid broad asset churn or unrelated refactors. Documentation-only work must not touch gameplay code, scenes, assets, materials, textures, project settings, or scripts.
