using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElementalHexTactics3D.Grid
{
    /// <summary>
    /// Represents axial (q, r) and cube (x, y, z) coordinates for 3D hexagonal grids.
    /// Uses Pointy-Top hex orientation with X as horizontal, Z as depth, and Y as vertical elevation.
    /// Cube coordinate constraint: x + y + z = 0 (where x = q, z = r, y = -q - r).
    /// </summary>
    [Serializable]
    public struct HexCoordinates : IEquatable<HexCoordinates>
    {
        [SerializeField] private int q;
        [SerializeField] private int r;

        public int Q => q;
        public int R => r;
        public int S => -q - r; // Cube coordinate Y component

        public HexCoordinates(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        #region World Position Conversion

        /// <summary>
        /// Converts these axial coordinates to 3D world space (XZ ground plane, Y elevation).
        /// </summary>
        public Vector3 ToWorldPosition(float hexRadius = 1f, float elevationHeight = 0.5f, int elevation = 0)
        {
            float x = hexRadius * Mathf.Sqrt(3f) * (q + r * 0.5f);
            float z = hexRadius * 1.5f * r;
            float y = elevation * elevationHeight;
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Converts a 3D world position into the nearest axial hex coordinates.
        /// </summary>
        public static HexCoordinates FromWorldPosition(Vector3 worldPos, float hexRadius = 1f)
        {
            float qFrac = (Mathf.Sqrt(3f) / 3f * worldPos.x - (1f / 3f) * worldPos.z) / hexRadius;
            float rFrac = ((2f / 3f) * worldPos.z) / hexRadius;
            float sFrac = -qFrac - rFrac;

            int roundQ = Mathf.RoundToInt(qFrac);
            int roundR = Mathf.RoundToInt(rFrac);
            int roundS = Mathf.RoundToInt(sFrac);

            float qDiff = Mathf.Abs(roundQ - qFrac);
            float rDiff = Mathf.Abs(roundR - rFrac);
            float sDiff = Mathf.Abs(roundS - sFrac);

            if (qDiff > rDiff && qDiff > sDiff)
            {
                roundQ = -roundR - roundS;
            }
            else if (rDiff > sDiff)
            {
                roundR = -roundQ - roundS;
            }

            return new HexCoordinates(roundQ, roundR);
        }

        #endregion

        #region Distance & Neighbors

        /// <summary>
        /// Calculates the grid step distance between this coordinate and another hex.
        /// </summary>
        public int DistanceTo(HexCoordinates other)
        {
            int dq = Mathf.Abs(q - other.q);
            int dr = Mathf.Abs(r - other.r);
            int ds = Mathf.Abs(S - other.S);
            return (dq + dr + ds) / 2;
        }

        /// <summary>
        /// The 6 axial direction vectors for Pointy-Top hexes.
        /// </summary>
        public static readonly HexCoordinates[] Directions = new HexCoordinates[]
        {
            new HexCoordinates( 1,  0), // East
            new HexCoordinates( 0,  1), // North-East
            new HexCoordinates(-1,  1), // North-West
            new HexCoordinates(-1,  0), // West
            new HexCoordinates( 0, -1), // South-West
            new HexCoordinates( 1, -1)  // South-East
        };

        public HexCoordinates GetNeighbor(int directionIndex)
        {
            int idx = ((directionIndex % 6) + 6) % 6;
            return this + Directions[idx];
        }

        public HexCoordinates[] GetNeighbors()
        {
            HexCoordinates[] neighbors = new HexCoordinates[6];
            for (int i = 0; i < 6; i++)
            {
                neighbors[i] = this + Directions[i];
            }
            return neighbors;
        }

        /// <summary>
        /// Returns all hex coordinates in a ring of a given radius around this center.
        /// </summary>
        public List<HexCoordinates> GetRing(int radius)
        {
            List<HexCoordinates> ring = new List<HexCoordinates>();
            if (radius <= 0)
            {
                ring.Add(this);
                return ring;
            }

            HexCoordinates current = this + Directions[4] * radius;
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < radius; j++)
                {
                    ring.Add(current);
                    current = current + Directions[i];
                }
            }
            return ring;
        }

        /// <summary>
        /// Returns all hex coordinates within a given radius range (inclusive).
        /// </summary>
        public List<HexCoordinates> GetRange(int maxRange)
        {
            List<HexCoordinates> results = new List<HexCoordinates>();
            for (int dq = -maxRange; dq <= maxRange; dq++)
            {
                int minR = Mathf.Max(-maxRange, -dq - maxRange);
                int maxR = Mathf.Min(maxRange, -dq + maxRange);
                for (int dr = minR; dr <= maxR; dr++)
                {
                    results.Add(new HexCoordinates(q + dq, r + dr));
                }
            }
            return results;
        }

        #endregion

        #region Operators & Overrides

        public static HexCoordinates operator +(HexCoordinates a, HexCoordinates b) => new HexCoordinates(a.q + b.q, a.r + b.r);
        public static HexCoordinates operator -(HexCoordinates a, HexCoordinates b) => new HexCoordinates(a.q - b.q, a.r - b.r);
        public static HexCoordinates operator *(HexCoordinates a, int scalar) => new HexCoordinates(a.q * scalar, a.r * scalar);
        public static bool operator ==(HexCoordinates a, HexCoordinates b) => a.q == b.q && a.r == b.r;
        public static bool operator !=(HexCoordinates a, HexCoordinates b) => !(a == b);

        public bool Equals(HexCoordinates other) => q == other.q && r == other.r;
        public override bool Equals(object obj) => obj is HexCoordinates other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(q, r);
        public override string ToString() => $"({q}, {r})";

        #endregion
    }
}

