using System.Collections.Generic;
using UnityEngine;

namespace ReinosDelEter
{
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

        [Header("Collider")]
        [Tooltip("Tamaño del BoxCollider que se agrega automáticamente a cada tile")]
        public Vector3 colliderSize = new Vector3(1f, 0.3f, 1f);

        // ── Runtime ──────────────────────────────────────────────────────────
        public List<Tile>[] paths { get; private set; }
        public Tile centerTile { get; private set; }
        public Tile[] startTiles { get; private set; }

        private List<Tile>[] _ringSides;
        private static readonly int[] RingOrder = { 0, 1, 3, 2 };

        private static readonly Vector3[] ArmDirs =
        {
            new Vector3(-1f, 0f,  1f).normalized,
            new Vector3( 1f, 0f,  1f).normalized,
            new Vector3(-1f, 0f, -1f).normalized,
            new Vector3( 1f, 0f, -1f).normalized,
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

            // Centro
            centerTile = SpawnTile(Vector3.zero, ElementType.Center, TileType.Center, -1, -1);

            // Brazos
            for (int p = 0; p < 4; p++)
            {
                paths[p] = new List<Tile>();
                Vector3 dir = ArmDirs[p];
                ElementType elem = (ElementType)p;

                for (int i = tilesPerArm; i >= 1; i--)
                {
                    TileType tt = i == tilesPerArm ? TileType.Start : TileType.Normal;
                    paths[p].Add(SpawnTile(dir * (i * armSpacing), elem, tt, p, i));
                }
                startTiles[p] = paths[p][0];

                if (castlePrefabs != null && p < castlePrefabs.Length && castlePrefabs[p] != null)
                {
                    var castle = Instantiate(castlePrefabs[p],
                        dir * ((tilesPerArm + 1.8f) * armSpacing),
                        Quaternion.identity, transform);
                    castle.transform.LookAt(Vector3.zero);
                }
            }

            // Anillo
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
                    Vector3 pos = Vector3.Lerp(posA, posB, t); pos.y = 0f;
                    ElementType elem = s <= half ? (ElementType)idxA : (ElementType)idxB;
                    Tile rt = SpawnTile(pos, elem, TileType.Normal, -1, -1);
                    rt.name = $"Ring_{idxA}to{idxB}_{s}";
                    _ringSides[r].Add(rt);
                }
            }

            BuildGraph();

            foreach (var st in startTiles)
                Debug.Log($"[BG] Castle {st.name} → {st.neighbors.Count} vecinos: " +
                          string.Join(", ", st.neighbors.ConvertAll(n => n.name)));
            Debug.Log($"[BG] Center → {centerTile.neighbors.Count} vecinos");
        }

        private void BuildGraph()
        {
            // Brazos
            for (int p = 0; p < 4; p++)
            {
                var arm = paths[p];
                for (int i = 0; i < arm.Count - 1; i++)
                {
                    arm[i].AddNeighbor(arm[i + 1]);
                    arm[i + 1].AddNeighbor(arm[i]);
                }
                arm[arm.Count - 1].AddNeighbor(centerTile);
                centerTile.AddNeighbor(arm[arm.Count - 1]);
            }

            // Anillo
            for (int r = 0; r < 4; r++)
            {
                int idxA = RingOrder[r];
                int idxB = RingOrder[(r + 1) % 4];
                List<Tile> seg = _ringSides[r];
                Tile ca = paths[idxA][0];
                Tile cb = paths[idxB][0];

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

        private Tile SpawnTile(Vector3 position, ElementType element,
                               TileType tileType, int pathIndex, int positionOnPath)
        {
            GameObject prefab = GetPrefabByElement(element);
            if (prefab == null)
            {
                Debug.LogError($"[BG] Sin prefab para {element}");
                return null;
            }

            // Respetamos la rotación del prefab (las tiles llevan -90° en X para
            // quedar planas). Con Quaternion.identity salían de pie.
            GameObject go = Instantiate(prefab, position, prefab.transform.rotation, transform);
            go.transform.localScale = tileScale;
            go.name = tileType == TileType.Center
                ? "Tile_Center"
                : $"Tile_{element}_P{pathIndex}_I{positionOnPath}";

            // ── Tile component ────────────────────────────────────────────────
            Tile tile = go.GetComponent<Tile>() ?? go.AddComponent<Tile>();
            tile.element = element;
            tile.tileType = tileType;
            tile.pathIndex = pathIndex;
            tile.positionOnPath = positionOnPath;

            // ── Collider — OBLIGATORIO para detectar clicks ───────────────────
            // Si el prefab ya tiene collider en algún hijo, lo dejamos.
            // Si no, añadimos un BoxCollider en el raíz.
            if (go.GetComponentInChildren<Collider>() == null)
            {
                BoxCollider col = go.AddComponent<BoxCollider>();
                col.size = colliderSize;
                col.center = Vector3.zero;
            }

            return tile;
        }

        private GameObject GetPrefabByElement(ElementType element) => element switch
        {
            ElementType.Fire => fireTilePrefab,
            ElementType.Water => waterTilePrefab,
            ElementType.Earth => earthTilePrefab,
            ElementType.Air => airTilePrefab,
            ElementType.Center => midTilePrefab,
            _ => null
        };

        private void ClearBoard()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}