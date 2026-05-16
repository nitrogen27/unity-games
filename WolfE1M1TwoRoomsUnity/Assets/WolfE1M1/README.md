# Wolf E1M1 Two Rooms Demo

This project is a compact Unity 6000.4.6f1 demo slice built from the opening area of `nitrogen27/wolfenstein3d_threejs`.

- Reference map: `LEVELS["0"]`, local bounds `x=26..42`, `y=0..8`.
- Scale: cell size `2`, wall height `2`, player eye height `0.9`.
- Texture source: `public/textures/walls.png`, atlas `16x16`, original tile `64x64`.
- Upscale: nearest-neighbor x4 to `256x256` extracted tiles, plus full `4096x4096` wall atlas.
- Runtime scope: first-person exploration, sliding doors, pixelated camera, simple `640x80` HUD.

No enemies, weapons, combat, pickups, scoring, or level progression are implemented.
