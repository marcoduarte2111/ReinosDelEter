using System.Collections.Generic;
using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// Generador del tablero de Reinos del Éter.
    /// Usa prefabs distintos por elemento:
    /// Fuego, Agua, Tierra y Aire.
    ///
    /// IMPORTANTE:
    /// Este script NO asigna colores a las tiles.
    /// Cada prefab debe traer su propio material/render con la temática definida.
    /// </summary>
    public class BoardGenerator : MonoBehaviour
    {
        [Header("Tile Prefabs por Elemento")]
        public GameObject fireTilePrefab;
        public GameObject waterTilePrefab;
        public GameObject earthTilePrefab;
        public GameObject airTilePrefab;

        [Header("Prefabs Especiales")]
        public GameObject midTilePrefab;
        public GameObject[] castlePrefabs;

        [Header("Layout")]
        [Range(3, 10)] public int tilesPerArm = 5;
        [Range(2, 8)] public int ringTilesPerSide = 4;
        public float armSpacing = 1.5f;

        [Header("Escala de las Tiles")]
        public Vector3 tileScale = Vector3.one;

        // ── Runtime ──────────────────────────────────────────────────────────
        public List<Tile>[] paths { get; private set; }
        public Tile centerTile { get; private set; }
        public Tile[] startTiles { get; private set; }

        private List<Tile>[] _ringSides;
        private static readonly int[] RingOrder = { 0, 1, 3, 2 };

        private static readonly Vector3[] ArmDirs =
        {
            new Vector3(-1f, 0f,  1f).normalized,  // 0 Water
            new Vector3( 1f, 0f,  1f).normalized,  // 1 Fire
            new Vector3(-1f, 0f, -1f).normalized,  // 2 Earth
            new Vector3( 1f, 0f, -1f).normalized,  // 3 Air
        };

        private void Awake()
        {
            GenerateBoard();
        }

        public Tile GetStartTile(int playerIndex)
        {
            return startTiles[playerIndex % 4];
        }

        public Tile GetStartTileByElement(ElementType element)
        {
            int idx = (int)element;

            if (paths == null || paths[idx] == null || paths[idx].Count == 0)
                return null;

            return paths[idx][0];
        }

        [ContextMenu("Generate Board")]
        public void GenerateBoard()
        {
            ClearBoard();

            paths = new List<Tile>[4];
            startTiles = new Tile[4];
            _ringSides = new List<Tile>[4];

            // ── 1. Centro ─────────────────────────────────────────────────────
            centerTile = SpawnTile(
                Vector3.zero,
                ElementType.Center,
                TileType.Center,
                -1,
                -1
            );

            // ── 2. Brazos ─────────────────────────────────────────────────────
            for (int p = 0; p < 4; p++)
            {
                paths[p] = new List<Tile>();

                Vector3 dir = ArmDirs[p];
                ElementType elem = (ElementType)p;

                for (int i = tilesPerArm; i >= 1; i--)
                {
                    float dist = i * armSpacing;

                    TileType tileType = i == tilesPerArm
                        ? TileType.Start
                        : TileType.Normal;

                    Tile tile = SpawnTile(
                        dir * dist,
                        elem,
                        tileType,
                        p,
                        i
                    );

                    paths[p].Add(tile);
                }

                startTiles[p] = paths[p][0];

                if (castlePrefabs != null && p < castlePrefabs.Length && castlePrefabs[p] != null)
                {
                    GameObject castle = Instantiate(
                        castlePrefabs[p],
                        dir * ((tilesPerArm + 1.8f) * armSpacing),
                        Quaternion.identity,
                        transform
                    );

                    castle.transform.LookAt(Vector3.zero);
                }
            }

            // ── 3. Anillo ─────────────────────────────────────────────────────
            for (int r = 0; r < 4; r++)
            {
                int idxA = RingOrder[r];
                int idxB = RingOrder[(r + 1) % 4];

                _ringSides[r] = new List<Tile>();

                Vector3 posA = paths[idxA][0].transform.position;
                Vector3 posB = paths[idxB][0].transform.position;

                int half = ringTilesPerSide / 2;

                for (int s = 1; s <= ringTilesPerSide; s++)
                {
                    float t = (float)s / (ringTilesPerSide + 1);

                    Vector3 pos = Vector3.Lerp(posA, posB, t);
                    pos.y = 0f;

                    ElementType elem = s <= half
                        ? (ElementType)idxA
                        : (ElementType)idxB;

                    Tile ringTile = SpawnTile(
                        pos,
                        elem,
                        TileType.Normal,
                        -1,
                        -1
                    );

                    ringTile.name = $"Ring_{idxA}to{idxB}_{s}";

                    _ringSides[r].Add(ringTile);
                }
            }

            BuildGraph();

            foreach (Tile startTile in startTiles)
            {
                Debug.Log($"[BG] Castle {startTile.name} → {startTile.neighbors.Count} vecinos: " +
                          string.Join(", ", startTile.neighbors.ConvertAll(n => n.name)));
            }

            Debug.Log($"[BG] Center → {centerTile.neighbors.Count} vecinos");
        }

        private void BuildGraph()
        {
            for (int p = 0; p < 4; p++)
            {
                List<Tile> arm = paths[p];

                for (int i = 0; i < arm.Count - 1; i++)
                {
                    arm[i].AddNeighbor(arm[i + 1]);
                    arm[i + 1].AddNeighbor(arm[i]);
                }

                arm[arm.Count - 1].AddNeighbor(centerTile);
                centerTile.AddNeighbor(arm[arm.Count - 1]);
            }

            for (int r = 0; r < 4; r++)
            {
                int idxA = RingOrder[r];
                int idxB = RingOrder[(r + 1) % 4];

                List<Tile> segment = _ringSides[r];

                Tile castleA = paths[idxA][0];
                Tile castleB = paths[idxB][0];

                castleA.AddNeighbor(segment[0]);
                segment[0].AddNeighbor(castleA);

                for (int s = 0; s < segment.Count - 1; s++)
                {
                    segment[s].AddNeighbor(segment[s + 1]);
                    segment[s + 1].AddNeighbor(segment[s]);
                }

                segment[segment.Count - 1].AddNeighbor(castleB);
                castleB.AddNeighbor(segment[segment.Count - 1]);
            }
        }

        private Tile SpawnTile(
            Vector3 position,
            ElementType element,
            TileType tileType,
            int pathIndex,
            int positionOnPath
        )
        {
            GameObject prefabToSpawn = GetPrefabByElement(element);

            if (prefabToSpawn == null)
            {
                Debug.LogError($"[BoardGenerator] No hay prefab asignado para el elemento: {element}");
                return null;
            }

            GameObject go = Instantiate(
                prefabToSpawn,
                position,
                Quaternion.identity,
                transform
            );

            go.transform.localScale = tileScale;

            go.name = tileType == TileType.Center
                ? "Tile_Center"
                : $"Tile_{element}_P{pathIndex}_I{positionOnPath}";

            Tile tile = go.GetComponent<Tile>();

            if (tile == null)
                tile = go.AddComponent<Tile>();

            tile.element = element;
            tile.tileType = tileType;
            tile.pathIndex = pathIndex;
            tile.positionOnPath = positionOnPath;

            // No se aplica ApplyElementVisuals().
            // El color/textura viene directamente desde el prefab asignado.

            return tile;
        }

        private GameObject GetPrefabByElement(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire:
                    return fireTilePrefab;

                case ElementType.Water:
                    return waterTilePrefab;

                case ElementType.Earth:
                    return earthTilePrefab;

                case ElementType.Air:
                    return airTilePrefab;

                case ElementType.Center:
                    return midTilePrefab;

                default:
                    Debug.LogWarning($"[BoardGenerator] Elemento no reconocido: {element}");
                    return null;
            }
        }

        private void ClearBoard()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
    }
}