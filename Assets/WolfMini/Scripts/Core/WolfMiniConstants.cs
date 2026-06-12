namespace WolfMini.Core
{
    public static class WolfMiniConstants
    {
        // WorldScale applies to authored environment modules only. Player and
        // actor sizes stay human-scale so the same Wolf layout reads as a taller
        // modern interior instead of a uniformly zoomed copy.
        public const float WorldScale = 1.8f;

        public const float CellSize = 2f * WorldScale;
        // Ceiling is decoupled from the cell: 4.2 m at a 1.68 m eye gives an
        // eye/ceiling ratio of 0.40 (Wolf was 0.45), so rooms actually read
        // taller instead of reproducing the old proportions at a bigger size.
        public const float WallHeight = 4.2f;
        // Wall texture module = full wall height: one floor-to-ceiling panel
        // per module, so the non-seamless panel texture never repeats
        // vertically and no horizontal joint can appear on a wall.
        public const float WallTextureModule = WallHeight;
        // Floor/ceiling module = one cell, matching the original grid look:
        // with the material tiling (0.5 / 0.25) the floor texture repeats every
        // two cells and the ceiling every four, proportional to the world.
        public const float FloorTextureModule = CellSize;
        // Square Wolf door: height equals the opening width (one cell); the
        // short band up to the ceiling is closed by the trim lintel.
        public const float DoorHeight = CellSize;
        public const float EyeHeight = 1.68f;
        public const float PlayerHeight = 1.75f;
        public const float PlayerRadius = 0.35f;
        public const float PlayerCenterY = PlayerHeight * 0.5f;
        public const float PlayerStepOffset = 0.30f;
        public const float UpperFloorY = 0f;
        public const float LowerFloorY = -3f;
        // Slab between stacked storeys: keeps the lower storey's ceiling,
        // lamps and door slabs clear of the upper floor plane, so nothing
        // z-fights or bleeds through seams at grazing angles.
        public const float FloorSlabThickness = 0.45f;
        public const int HudBaseWidth = 640;
        public const int HudBaseHeight = 80;
        public const float DoorThickness = 0.22f;
        public const float DoorTravel = CellSize - 0.02f;
        public const float DoorPassableOpenAmount = 0.8f;
    }
}
