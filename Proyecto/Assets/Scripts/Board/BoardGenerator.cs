using System.Collections.Generic;
using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// Tablero de Reinos del Éter — grafo explícito sin nodos artificiales.
    ///
    /// REGLAS DEL GRAFO:
    ///   • Cada tile del brazo conecta SOLO con el anterior y el siguiente del brazo
    ///   • El tile más cercano al centro (arm[last]) conecta además con el centro
    ///   • El castillo (arm[0]) conecta con arm[1] + los 2 tiles del anillo adyacentes
    ///   • Los tiles del anillo conectan solo con sus vecinos del anillo
    ///   • El centro conecta con los 4 tiles arm[last]
    ///
    /// RESULTADO:
    ///   • Tiles del brazo intermedios: siempre 2 vecinos → avanzan automático
    ///   • Castillo: 3 vecinos → pide dirección al inicio
    ///   • arm[last]: 2 vecinos (arm[last-1] + centro) → avanzan automático
    ///   • Centro: 4 vecinos → pide dirección
    ///   • Tiles del anillo intermedios: 2 vecinos → avanzan automático
    /// </summary>
    public class BoardGenerator : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject tilePrefab;
        public GameObject centerPrefab;
        public GameObject[] castlePrefabs;

        [Header("Layout")]
        [Range(3, 10)] public int tilesPerArm = 5;
        [Range(2, 8)] public int ringTilesPerSide = 4;
        public float armSpacing = 1.5f;
        public float tileWidth = 1.0f;
        public float tileHeight = 0.22f;

        [Header("Visual")]
        public bool enableEmission = true;

        // ── Runtime ──────────────────────────────────────────────────────────
        public List<Tile>[] paths { get; private set; }
        public Tile centerTile { get; private set; }
        public Tile[] startTiles { get; private set; }

        // Ring sides: _ringSides[r] = tiles entre castillo[RingOrder[r]] y castillo[RingOrder[r+1]]
        private List<Tile>[] _ringSides;
        private static readonly int[] RingOrder = { 0, 1, 3, 2 };

        private static readonly Vector3[] ArmDirs =
        {
            new Vector3(-1f, 0f,  1f).normalized,  // 0 Water
            new Vector3( 1f, 0f,  1f).normalized,  // 1 Fire
            new Vector3(-1f, 0f, -1f).normalized,  // 2 Earth
            new Vector3( 1f, 0f, -1f).normalized,  // 3 Air
        };

        private void Awake() => GenerateBoard();

        public Tile GetStartTile(int playerIndex) => startTiles[playerIndex % 4];

        public Tile GetStartTileByElement(ElementType element)
        {
            int idx = (int)element;
            if (paths == null || paths[idx] == null || paths[idx].Count == 0) return null;
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
            centerTile = SpawnTile(Vector3.zero, ElementType.Center, TileType.Center, -1, -1);
            if (centerPrefab != null)
                Instantiate(centerPrefab, Vector3.zero, Quaternion.identity, transform);

            // ── 2. Brazos ─────────────────────────────────────────────────────
            // paths[p][0] = castillo, paths[p][last] = más cercano al centro
            for (int p = 0; p < 4; p++)
            {
                paths[p] = new List<Tile>();
                Vector3 dir = ArmDirs[p];
                ElementType elem = (ElementType)p;

                for (int i = tilesPerArm; i >= 1; i--)
                {
                    float dist = i * armSpacing;
                    TileType tt = (i == tilesPerArm) ? TileType.Start : TileType.Normal;
                    Tile tile = SpawnTile(dir * dist, elem, tt, p, i);
                    paths[p].Add(tile);
                }
                startTiles[p] = paths[p][0];

                if (castlePrefabs != null && p < castlePrefabs.Length && castlePrefabs[p] != null)
                {
                    var c = Instantiate(castlePrefabs[p],
                        dir * ((tilesPerArm + 1.8f) * armSpacing),
                        Quaternion.identity, transform);
                    c.transform.LookAt(Vector3.zero);
                }
            }

            // ── 3. Anillo ─────────────────────────────────────────────────────
            // El anillo va entre los castillos adyacentes
            for (int r = 0; r < 4; r++)
            {
                int idxA = RingOrder[r];
                int idxB = RingOrder[(r + 1) % 4];
                _ringSides[r] = new List<Tile>();

                Vector3 posA = paths[idxA][0].transform.position; // castillo A
                Vector3 posB = paths[idxB][0].transform.position; // castillo B

                int half = ringTilesPerSide / 2;
                for (int s = 1; s <= ringTilesPerSide; s++)
                {
                    float t = (float)s / (ringTilesPerSide + 1);
                    Vector3 pos = Vector3.Lerp(posA, posB, t);
                    pos.y = 0f;
                    ElementType elem = s <= half ? (ElementType)idxA : (ElementType)idxB;
                    Tile rt = SpawnTile(pos, elem, TileType.Normal, -1, -1);
                    rt.name = $"Ring_{idxA}to{idxB}_{s}";
                    _ringSides[r].Add(rt);
                }
            }

            // ── 4. Grafo explícito ────────────────────────────────────────────
            BuildGraph();

            // Log para verificar
            foreach (var st in startTiles)
                Debug.Log($"[BG] Castle {st.name} → {st.neighbors.Count} vecinos: " +
                          string.Join(", ", st.neighbors.ConvertAll(n => n.name)));
            Debug.Log($"[BG] Center → {centerTile.neighbors.Count} vecinos");
        }

        private void BuildGraph()
        {
            // ── Brazos: chain lineal ──────────────────────────────────────────
            // paths[p][0]=castillo ... paths[p][last]=junto al centro
            // Solo conecta tiles consecutivos del brazo
            for (int p = 0; p < 4; p++)
            {
                var arm = paths[p];
                for (int i = 0; i < arm.Count - 1; i++)
                {
                    arm[i].AddNeighbor(arm[i + 1]);
                    arm[i + 1].AddNeighbor(arm[i]);
                }
                // Último tile del brazo ↔ centro
                arm[arm.Count - 1].AddNeighbor(centerTile);
                centerTile.AddNeighbor(arm[arm.Count - 1]);
            }

            // ── Anillo: chain lineal entre castillos ──────────────────────────
            for (int r = 0; r < 4; r++)
            {
                int idxA = RingOrder[r];
                int idxB = RingOrder[(r + 1) % 4];
                var seg = _ringSides[r];
                Tile ca = paths[idxA][0]; // castillo A
                Tile cb = paths[idxB][0]; // castillo B

                // castillo A ↔ seg[0] ↔ ... ↔ seg[last] ↔ castillo B
                ca.AddNeighbor(seg[0]);
                seg[0].AddNeighbor(ca);

                for (int s = 0; s < seg.Count - 1; s++)
                {
                    seg[s].AddNeighbor(seg[s + 1]);
                    seg[s + 1].AddNeighbor(seg[s]);
                }

                seg[seg.Count - 1].AddNeighbor(cb);
                cb.AddNeighbor(seg[seg.Count - 1]);
            }
        }

        private Tile SpawnTile(Vector3 pos, ElementType elem, TileType tType, int pIdx, int posIdx)
        {
            if (tilePrefab == null) { Debug.LogError("[BG] tilePrefab null"); return null; }
            var go = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
            go.transform.localScale = new Vector3(tileWidth, tileHeight, tileWidth);
            go.name = tType == TileType.Center ? "Tile_Center"
                    : $"Tile_{elem}_P{pIdx}_I{posIdx}";
            Tile tile = go.GetComponent<Tile>() ?? go.AddComponent<Tile>();
            tile.element = elem;
            tile.tileType = tType;
            tile.pathIndex = pIdx;
            tile.positionOnPath = posIdx;
            if (enableEmission) tile.ApplyElementVisuals();
            return tile;
        }

        private void ClearBoard()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}