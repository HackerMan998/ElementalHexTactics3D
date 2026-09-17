using System.Collections.Generic;
using UnityEngine;

namespace ElementalHexTactics3D.Grid
{
    /// <summary>
    /// Master grid manager that procedurally generates and tracks the 3D hexagonal battlefield.
    /// Manages the elemental material library so every terrain state displays its authentic square texture.
    /// </summary>
    public class HexGrid3D : MonoBehaviour
    {
        public static HexGrid3D Instance { get; private set; }

        [Header("Grid Dimensions")]
        [SerializeField] private int gridRadius = 4;
        [SerializeField] private float hexRadius = 1.0f;
        [SerializeField] private float elevationHeight = 0.6f;
        [SerializeField] private float pillarDepth = 1.2f;

        [Header("Elemental Materials Library (Square Textures)")]
        [SerializeField] private Material barrenMaterial;
        [SerializeField] private Material grassMaterial;
        [SerializeField] private Material scorchedMaterial;
        [SerializeField] private Material magmaMaterial;
        [SerializeField] private Material shallowWaterMaterial;
        [SerializeField] private Material deepWaterMaterial;
        [SerializeField] private Material steamMaterial;
        [SerializeField] private Material sidePillarMaterial;

        [Header("Initial Elevation & Biome Settings")]
        [SerializeField] private bool generateCenterPlateau = true;
        [SerializeField] private bool generateSampleBiomes = true;

        private readonly Dictionary<HexCoordinates, HexTile3D> tiles = new Dictionary<HexCoordinates, HexTile3D>();
        private Mesh sharedPillarMesh;

        public int GridRadius => gridRadius;
        public float HexRadius => hexRadius;
        public float ElevationHeight => elevationHeight;
        public IReadOnlyDictionary<HexCoordinates, HexTile3D> Tiles => tiles;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            RegisterExistingTiles();
        }

        private void Start()
        {
            if (tiles.Count == 0)
            {
                RegisterExistingTiles();
            }

            if (tiles.Count == 0)
            {
                GenerateGrid();
            }
        }

        /// <summary>
        /// Registers existing HexTile3D children already in the scene, preventing accidental destruction on play.
        /// </summary>
        public void RegisterExistingTiles()
        {
            tiles.Clear();
            HexTile3D[] existingTiles = GetComponentsInChildren<HexTile3D>(true);
            foreach (var tile in existingTiles)
            {
                if (tile != null)
                {
                    tiles[tile.Coordinates] = tile;
                }
            }
            if (tiles.Count > 0)
            {
                Debug.Log($"<color=#4CAF50><b>[HexGrid3D]</b></color> Registered {tiles.Count} existing tiles from scene.");
            }
        }

        public Material GetTopMaterial(TileState state, int tier = 1)
        {
            switch (state)
            {
                case TileState.Barren:
                    return barrenMaterial;
                case TileState.Grass:
                    return grassMaterial ?? barrenMaterial;
                case TileState.Scorched:
                    return scorchedMaterial ?? barrenMaterial;
                case TileState.Magma:
                    return magmaMaterial ?? scorchedMaterial ?? barrenMaterial;
                case TileState.Water:
                    return (tier >= 2) ? (deepWaterMaterial ?? shallowWaterMaterial ?? barrenMaterial)
                                       : (shallowWaterMaterial ?? barrenMaterial);
                case TileState.Steam:
                    return steamMaterial ?? barrenMaterial;
                default:
                    return barrenMaterial;
            }
        }

        [ContextMenu("Regenerate Grid")]
        public void GenerateGrid()
        {
            ClearGrid();

            // Generate or cache procedural 3D hex pillar mesh with edge-to-edge square UV mapping
            if (sharedPillarMesh == null)
            {
                sharedPillarMesh = HexMeshBuilder.CreateHexPillarMesh(hexRadius, pillarDepth);
            }

            HexCoordinates origin = new HexCoordinates(0, 0);
            List<HexCoordinates> allCoords = origin.GetRange(gridRadius);

            GameObject container = new GameObject("Tiles_Container");
            container.transform.SetParent(transform, false);

            foreach (var coord in allCoords)
            {
                int elev = 0;
                if (generateCenterPlateau)
                {
                    int dist = coord.DistanceTo(origin);
                    if (dist <= 1) elev = 2;
                    else if (dist <= 2) elev = 1;
                }

                Vector3 worldPos = coord.ToWorldPosition(hexRadius, elevationHeight, elev);

                GameObject tileObj = new GameObject($"HexTile_{coord.Q}_{coord.R}");
                tileObj.transform.SetParent(container.transform, false);
                tileObj.transform.position = worldPos;

                HexTile3D tile = tileObj.AddComponent<HexTile3D>();

                // Determine state for this tile
                TileState state = TileState.Barren;
                int tier = 0;

                if (generateSampleBiomes)
                {
                    DetermineSampleBiome(coord, out state, out tier);
                }

                Material topMat = GetTopMaterial(state, tier);
                tile.Initialize(coord, elev, sharedPillarMesh, topMat, sidePillarMaterial);
                tile.SetState(state, tier);

                tiles[coord] = tile;
            }

            Debug.Log($"<color=#4CAF50><b>[HexGrid3D]</b></color> Generated {tiles.Count} 3D hex tiles (Radius: {gridRadius}).");
        }

        private void DetermineSampleBiome(HexCoordinates coord, out TileState state, out int tier)
        {
            int dist = coord.DistanceTo(new HexCoordinates(0, 0));
            if (dist == 0)
            {
                state = TileState.Grass;
                tier = 1;
            }
            else if (coord.Q > 1 && coord.R < 0)
            {
                // North-East / East sector: Scorched & Magma
                if (coord.Q == 2 && coord.R == -1)
                {
                    state = TileState.Magma;
                    tier = 2;
                }
                else
                {
                    state = TileState.Scorched;
                    tier = 1;
                }
            }
            else if (coord.Q < -1 && coord.R > 0)
            {
                // South-West / West sector: Shallow & Deep Water
                if (coord.Q == -2 && coord.R == 2)
                {
                    state = TileState.Water;
                    tier = 2;
                }
                else
                {
                    state = TileState.Water;
                    tier = 1;
                }
            }
            else if (dist <= 2)
            {
                state = TileState.Grass;
                tier = 1;
            }
            else
            {
                state = TileState.Barren;
                tier = 0;
            }
        }

        public void ClearGrid()
        {
            tiles.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        public HexTile3D GetTile(HexCoordinates coords)
        {
            tiles.TryGetValue(coords, out HexTile3D tile);
            return tile;
        }

        public List<HexTile3D> GetNeighbors(HexCoordinates coords)
        {
            List<HexTile3D> neighbors = new List<HexTile3D>();
            foreach (var dir in HexCoordinates.Directions)
            {
                HexCoordinates neighborCoord = coords + dir;
                if (tiles.TryGetValue(neighborCoord, out HexTile3D neighborTile))
                {
                    neighbors.Add(neighborTile);
                }
            }
            return neighbors;
        }

        public List<HexTile3D> GetTilesInRange(HexCoordinates center, int range)
        {
            List<HexTile3D> results = new List<HexTile3D>();
            List<HexCoordinates> coords = center.GetRange(range);
            foreach (var c in coords)
            {
                if (tiles.TryGetValue(c, out HexTile3D tile))
                {
                    results.Add(tile);
                }
            }
            return results;
        }
    }
}
