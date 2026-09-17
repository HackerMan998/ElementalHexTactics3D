using System.Collections.Generic;
using UnityEngine;

namespace ElementalHexTactics3D.Grid
{
    /// <summary>
    /// Pathfinding & Reachable Movement calculations for 3D hexagonal grids,
    /// supporting terrain costs, elevation step limits (cliffs), and occupancy checks.
    /// </summary>
    public static class HexPathfinder3D
    {
        public struct PathNode
        {
            public HexTile3D Tile;
            public int RemainingMove;
        }

        /// <summary>
        /// Calculates all reachable hex tiles from a starting tile within the unit's movement range.
        /// </summary>
        public static HashSet<HexTile3D> GetReachableTiles(
            HexGrid3D grid,
            HexTile3D startTile,
            int moveRange,
            int maxClimbHeight = 1)
        {
            HashSet<HexTile3D> reachable = new HashSet<HexTile3D>();
            if (grid == null || startTile == null || moveRange <= 0) return reachable;

            Dictionary<HexTile3D, int> costSoFar = new Dictionary<HexTile3D, int>();
            Queue<HexTile3D> frontier = new Queue<HexTile3D>();

            frontier.Enqueue(startTile);
            costSoFar[startTile] = 0;

            while (frontier.Count > 0)
            {
                HexTile3D current = frontier.Dequeue();
                int currentCost = costSoFar[current];

                foreach (var neighbor in grid.GetNeighbors(current.Coordinates))
                {
                    if (neighbor == null) continue;

                    // Cannot leap up/down cliffs higher than maxClimbHeight
                    int elevDiff = Mathf.Abs(neighbor.Elevation - current.Elevation);
                    if (elevDiff > maxClimbHeight) continue;

                    // Check terrain movement cost
                    int tileCost = GetTileMovementCost(neighbor);
                    if (tileCost < 0) continue; // Impassable (e.g. Deep Water)

                    int newCost = currentCost + tileCost;
                    if (newCost > moveRange) continue;

                    // Cannot walk through enemy/occupied tiles
                    if (neighbor.IsOccupied && neighbor != startTile) continue;

                    if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                    {
                        costSoFar[neighbor] = newCost;
                        frontier.Enqueue(neighbor);
                        reachable.Add(neighbor);
                    }
                }
            }

            return reachable;
        }

        /// <summary>
        /// Calculates the shortest path between startTile and targetTile using A* search.
        /// </summary>
        public static List<HexTile3D> FindPath(
            HexGrid3D grid,
            HexTile3D startTile,
            HexTile3D targetTile,
            int maxClimbHeight = 1)
        {
            List<HexTile3D> path = new List<HexTile3D>();
            if (grid == null || startTile == null || targetTile == null) return path;
            if (startTile == targetTile) return path;

            Dictionary<HexTile3D, HexTile3D> cameFrom = new Dictionary<HexTile3D, HexTile3D>();
            Dictionary<HexTile3D, int> costSoFar = new Dictionary<HexTile3D, int>();
            PriorityQueue<HexTile3D, float> frontier = new PriorityQueue<HexTile3D, float>();

            frontier.Enqueue(startTile, 0);
            cameFrom[startTile] = null;
            costSoFar[startTile] = 0;

            while (frontier.Count > 0)
            {
                HexTile3D current = frontier.Dequeue();

                if (current == targetTile) break;

                foreach (var neighbor in grid.GetNeighbors(current.Coordinates))
                {
                    if (neighbor == null) continue;

                    int elevDiff = Mathf.Abs(neighbor.Elevation - current.Elevation);
                    if (elevDiff > maxClimbHeight) continue;

                    int moveCost = GetTileMovementCost(neighbor);
                    if (moveCost < 0) continue;

                    // Cannot stop on or walk through occupied tile (except target tile if checking interaction)
                    if (neighbor.IsOccupied && neighbor != targetTile) continue;

                    int newCost = costSoFar[current] + moveCost;
                    if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                    {
                        costSoFar[neighbor] = newCost;
                        float priority = newCost + current.Coordinates.DistanceTo(targetTile.Coordinates);
                        frontier.Enqueue(neighbor, priority);
                        cameFrom[neighbor] = current;
                    }
                }
            }

            if (!cameFrom.ContainsKey(targetTile)) return path; // No path found

            // Reconstruct path backwards
            HexTile3D currStep = targetTile;
            while (currStep != null && currStep != startTile)
            {
                path.Add(currStep);
                cameFrom.TryGetValue(currStep, out currStep);
            }

            path.Reverse();
            return path;
        }

        public static int GetTileMovementCost(HexTile3D tile)
        {
            if (tile == null) return -1;

            // Deep Water (Tier 2) is impassable to walking units
            if (tile.State == TileState.Water && tile.TierLevel >= 2)
            {
                return -1;
            }

            // Magma (Tier 2) is passable but dangerous (1 cost)
            if (tile.State == TileState.Magma)
            {
                return 1;
            }

            // Standard terrain
            return 1;
        }

        #region Helper Simple Priority Queue

        private class PriorityQueue<TItem, TPriority> where TPriority : System.IComparable<TPriority>
        {
            private readonly List<KeyValuePair<TItem, TPriority>> elements = new List<KeyValuePair<TItem, TPriority>>();

            public int Count => elements.Count;

            public void Enqueue(TItem item, TPriority priority)
            {
                elements.Add(new KeyValuePair<TItem, TPriority>(item, priority));
            }

            public TItem Dequeue()
            {
                int bestIndex = 0;
                for (int i = 1; i < elements.Count; i++)
                {
                    if (elements[i].Value.CompareTo(elements[bestIndex].Value) < 0)
                    {
                        bestIndex = i;
                    }
                }
                TItem bestItem = elements[bestIndex].Key;
                elements.RemoveAt(bestIndex);
                return bestItem;
            }
        }

        #endregion
    }
}

