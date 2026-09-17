using UnityEngine;

namespace ElementalHexTactics3D.Grid
{
    /// <summary>
    /// Represents the elemental and physical state of a hex tile.
    /// </summary>
    public enum TileState
    {
        Barren,    // Neutral dry earth (Tier 0)
        Scorched,  // Fire Tier 1
        Magma,     // Fire Tier 2
        Water,     // Water (Tier 1 = Shallow, Tier 2 = Deep)
        Steam,     // Vapor cloud (Fire + Water reaction)
        Grass      // Nature base
    }

    /// <summary>
    /// Utility methods for TileState color & visual properties.
    /// </summary>
    public static class TileStateExtensions
    {
        public static Color GetDefaultColor(this TileState state, int tier = 1)
        {
            switch (state)
            {
                case TileState.Barren:
                    return new Color(0.72f, 0.62f, 0.50f, 1f); // Warm earthy neutral

                case TileState.Scorched:
                    return new Color(0.42f, 0.20f, 0.14f, 1f); // Dark charred ash

                case TileState.Magma:
                    return new Color(1.00f, 0.35f, 0.05f, 1f); // Glowing molten orange

                case TileState.Water:
                    return (tier >= 2)
                        ? new Color(0.08f, 0.35f, 0.78f, 1f)  // Deep Water
                        : new Color(0.20f, 0.62f, 0.95f, 1f); // Shallow Water

                case TileState.Steam:
                    return new Color(0.88f, 0.92f, 0.98f, 0.9f); // Misty white vapor

                case TileState.Grass:
                    return new Color(0.35f, 0.70f, 0.30f, 1f); // Lush vibrant green

                default:
                    return Color.white;
            }
        }
    }
}

