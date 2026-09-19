using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElementalHexTactics3D.Grid;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.CameraControl;

namespace ElementalHexTactics3D.Combat
{
    /// <summary>
    /// Executes Into the Breach-style push / displacement attacks on 3D hex grids.
    /// Displaces the target 1 hex directly away from the caster, triggering hazard & collision damage.
    /// </summary>
    public static class PushMechanic3D
    {
        /// <summary>
        /// Awaited push coroutine: applies base damage, displaces target smoothly, and resolves hazards on arrival.
        /// </summary>
        public static IEnumerator ExecutePushRoutine(TacticalUnit3D caster, TacticalUnit3D target, HexGrid3D grid)
        {
            if (caster == null || target == null || grid == null) yield break;

            HexCoordinates casterCoords = caster.Coordinates;
            HexCoordinates targetCoords = target.Coordinates;

            // Find the best directional hex vector from caster to target
            HexCoordinates pushDir = GetPushDirection(casterCoords, targetCoords);
            HexCoordinates destCoords = targetCoords + pushDir;

            HexTile3D destTile = grid.GetTile(destCoords);

            Debug.Log($"<color=#7E57C2><b>[Kinetic Push]</b></color> {caster.UnitName} pushed {target.UnitName} towards {destCoords}!");

            // 1. Base push damage
            target.TakeDamage(1, "💨 PUSH! -1");
            if (target == null || target.CurrentHealth <= 0) yield break;

            // 2. CASE 1: BLOCKED (Off-grid edge, occupied tile, or steep cliff > 1)
            bool isBlocked = (destTile == null) ||
                             destTile.IsOccupied ||
                             (target.CurrentTile != null && Mathf.Abs(destTile.Elevation - target.CurrentTile.Elevation) > 1);

            if (isBlocked)
            {
                Debug.Log($"<color=#EF5350><b>[Wall Slam!]</b></color> {target.UnitName}'s push was blocked! Slammed into obstacle for 2 collision damage.");
                yield return new WaitForSeconds(0.15f);
                TacticalCameraController.Instance?.Shake(0.32f, 0.35f);
                SoundManager3D.Instance?.PlaySlam(1.3f);
                Vector3 slamPos = target.transform.position + Vector3.up * 0.5f;
                CombatVFXManager.Instance?.PlayWallSlam(slamPos, Vector3.up);
                if (target != null && target.CurrentHealth > 0)
                {
                    target.TakeDamage(2, "💥 WALL SLAM! -2");
                }
                yield break;
            }

            // 3. CASE 2: SUCCESSFUL DISPLACEMENT (Await unit moving to destination)
            if (target == null || target.CurrentHealth <= 0) yield break;

            List<HexTile3D> pushPath = new List<HexTile3D> { destTile };
            yield return target.MoveAlongPath(pushPath, stepDuration: 0.18f);

            if (target == null || target.CurrentHealth <= 0) yield break;

            // 4. Check Environmental Hazards on destination tile upon landing
            if (destTile.State == TileState.Magma)
            {
                Debug.Log($"<color=#FF3D00><b>[Hazard Ignition!]</b></color> {target.UnitName} was pushed into molten Magma! Took 3 bonus burn damage.");
                yield return new WaitForSeconds(0.1f);
                TacticalCameraController.Instance?.Shake(0.35f, 0.40f);
                SoundManager3D.Instance?.PlaySpellCast(isFire: true);
                CombatVFXManager.Instance?.PlayFireBurst(destTile.GetTopCenterPosition());
                if (target != null && target.CurrentHealth > 0)
                {
                    target.TakeDamage(3, "🔥 MAGMA BURN! -3");
                }
            }
            else if (destTile.State == TileState.Water && destTile.TierLevel >= 2)
            {
                Debug.Log($"<color=#0288D1><b>[Deep Water Submersion!]</b></color> {target.UnitName} was plunged into Deep Water! Took 2 bonus hazard damage.");
                yield return new WaitForSeconds(0.1f);
                TacticalCameraController.Instance?.Shake(0.22f, 0.25f);
                SoundManager3D.Instance?.PlaySpellCast(isFire: false);
                CombatVFXManager.Instance?.PlayWaterSplash(destTile.GetTopCenterPosition());
                if (target != null && target.CurrentHealth > 0)
                {
                    target.TakeDamage(2, "🌊 WATER PLUNGE! -2");
                }
            }
        }

        public static bool ExecutePush(TacticalUnit3D caster, TacticalUnit3D target, HexGrid3D grid, MonoBehaviour coroutineRunner)
        {
            if (caster == null || target == null || grid == null) return false;
            if (coroutineRunner != null)
            {
                coroutineRunner.StartCoroutine(ExecutePushRoutine(caster, target, grid));
            }
            else
            {
                caster.StartCoroutine(ExecutePushRoutine(caster, target, grid));
            }
            return true;
        }

        private static HexCoordinates GetPushDirection(HexCoordinates from, HexCoordinates to)
        {
            HexCoordinates diff = to - from;

            // If adjacent, diff is directly one of the 6 directions
            foreach (var dir in HexCoordinates.Directions)
            {
                if (dir == diff) return dir;
            }

            // Fallback: pick the direction closest to the vector
            HexCoordinates bestDir = HexCoordinates.Directions[0];
            int minDistance = int.MaxValue;

            foreach (var dir in HexCoordinates.Directions)
            {
                int dist = diff.DistanceTo(dir);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestDir = dir;
                }
            }

            return bestDir;
        }
    }
}

