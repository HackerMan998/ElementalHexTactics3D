using UnityEngine;
using ElementalHexTactics3D.Grid;
using ElementalHexTactics3D.Units;

namespace ElementalHexTactics3D.Combat
{
    /// <summary>
    /// Result data returned when predicting or applying a terrain reaction.
    /// Reused by both actual spell casting and the Ghost UI preview to prevent desyncs.
    /// </summary>
    public struct TerrainReactionResult
    {
        public TileState ResultingState;
        public int ResultingTier;
        public string ReactionName;
        public bool TriggeredReaction;
        public string Description;
    }

    /// <summary>
    /// Core system evaluating elemental interactions on hex terrain.
    /// Follows the 3-step resolution:
    ///   1. Does the spell OPPOSE the tile's current state? (trigger reset / elemental reaction)
    ///   2. Does the spell MATCH the tile's current state? (upgrade TierLevel)
    ///   3. Is the tile neutral? (set new elemental state)
    /// </summary>
    public static class TerrainReactionSystem
    {
        /// <summary>
        /// Pure prediction method: calculates the outcome without modifying the tile.
        /// Powers the Ghost UI preview.
        /// </summary>
        public static TerrainReactionResult PredictReaction(TileState currentState, int currentTier, ElementType spell)
        {
            TerrainReactionResult result = new TerrainReactionResult
            {
                ResultingState = currentState,
                ResultingTier = currentTier,
                ReactionName = "No Reaction",
                TriggeredReaction = false,
                Description = "Terrain unaffected."
            };

            switch (spell)
            {
                case ElementType.Fire:
                    // 1. OPPOSE Check: Fire on Water -> STEAM CLOUD!
                    if (currentState == TileState.Water)
                    {
                        result.ResultingState = TileState.Steam;
                        result.ResultingTier = 1;
                        result.ReactionName = "Steam Cloud";
                        result.TriggeredReaction = true;
                        result.Description = "Scalding Steam Cloud erupted! Water boiled away into vapor!";
                        return result;
                    }

                    // 2. MATCH Check: Fire matches Scorched Earth -> upgrades to Magma
                    if (currentState == TileState.Scorched)
                    {
                        result.ResultingState = TileState.Magma;
                        result.ResultingTier = 2;
                        result.ReactionName = "Magma Surge";
                        result.TriggeredReaction = true;
                        result.Description = "Scorched earth melts and intensifies into molten Magma (Tier 2)!";
                        return result;
                    }
                    if (currentState == TileState.Magma)
                    {
                        result.ResultingState = TileState.Magma;
                        result.ResultingTier = 2;
                        result.ReactionName = "Intense Heat";
                        result.TriggeredReaction = false;
                        result.Description = "Magma roars with continuous flame!";
                        return result;
                    }

                    // 3. NEUTRAL Check: Fire infuses Barren/Grass/Steam -> Scorched Earth
                    if (currentState == TileState.Barren || currentState == TileState.Grass || currentState == TileState.Steam)
                    {
                        result.ResultingState = TileState.Scorched;
                        result.ResultingTier = 1;
                        result.ReactionName = "Scorched Earth";
                        result.TriggeredReaction = true;
                        result.Description = "Earth is charred into Scorched Earth (Tier 1)!";
                        return result;
                    }
                    break;

                case ElementType.Water:
                    // 1. OPPOSE Check: Water on Magma / Scorched -> Quench & Cool!
                    if (currentState == TileState.Magma)
                    {
                        result.ResultingState = TileState.Barren;
                        result.ResultingTier = 0;
                        result.ReactionName = "Quench & Solidify";
                        result.TriggeredReaction = true;
                        result.Description = "Molten magma quenched with explosive steam into solid barren rock!";
                        return result;
                    }
                    if (currentState == TileState.Scorched)
                    {
                        result.ResultingState = TileState.Barren;
                        result.ResultingTier = 0;
                        result.ReactionName = "Ash Extinguished";
                        result.TriggeredReaction = true;
                        result.Description = "Water washed the scorching ash into cool earth.";
                        return result;
                    }

                    // 2. MATCH Check: Water matches Water -> upgrades to Deep Water (Tier 2)
                    if (currentState == TileState.Water)
                    {
                        result.ResultingState = TileState.Water;
                        result.ResultingTier = 2;
                        result.ReactionName = "Deep Water Surge";
                        result.TriggeredReaction = true;
                        result.Description = "Water level deepens into treacherous Deep Water (Tier 2)!";
                        return result;
                    }

                    // 3. NEUTRAL Check: Water infuses Barren/Grass/Steam -> Shallow Water (Tier 1)
                    if (currentState == TileState.Barren || currentState == TileState.Grass || currentState == TileState.Steam)
                    {
                        result.ResultingState = TileState.Water;
                        result.ResultingTier = 1;
                        result.ReactionName = "Water Inundation";
                        result.TriggeredReaction = true;
                        result.Description = "Ground flooded with Shallow Water (Tier 1).";
                        return result;
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// Executes a spell on a target hex tile:
        /// 1. Updates terrain state and tier.
        /// 2. Damages occupant unit.
        /// </summary>
        public static TerrainReactionResult ApplySpell(HexTile3D tile, ElementType spell, int damage = 3)
        {
            if (tile == null) return default;

            // 1. Evaluate and apply terrain reaction
            TerrainReactionResult reaction = PredictReaction(tile.State, tile.TierLevel, spell);
            tile.SetState(reaction.ResultingState, reaction.ResultingTier);

            // 2. Damage unit occupying the tile & refresh attunement
            TacticalUnit3D occupant = tile.CurrentOccupant as TacticalUnit3D;
            if (occupant != null)
            {
                occupant.UpdateAttunement();
                string spellTag = (spell == ElementType.Fire) ? "🔥 FIREBALL!" : "💧 WATER!";
                occupant.TakeDamage(damage, $"{spellTag} -{damage}");
                Debug.Log($"<color=#FF5252><b>[Direct Hit!]</b></color> {spell} dealt <b>{damage}</b> damage to <b>{occupant.UnitName}</b>!");
            }
            else
            {
                Debug.Log($"<color=#BDBDBD>[Spell Infusion]</color> {spell} struck empty tile {tile.Coordinates}.");
            }

            Debug.Log($"<color=#FFA726><b>[Spell Cast]</b></color> {spell} on {tile.Coordinates} → " +
                      $"<b>{reaction.ReactionName}</b> ({tile.State}, Tier {tile.TierLevel}). {reaction.Description}");

            return reaction;
        }
    }
}

