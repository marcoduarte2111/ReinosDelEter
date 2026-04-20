using System.Collections.Generic;
using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// Genera el tablero de Reinos del Éter:
    ///
    ///   Estructura:
    ///   • 4 brazos diagonales (X) que van desde cada castillo al centro
    ///   • Anillo exterior cuadrado que conecta los 4 brazos entre sí
    ///   • Casilla central (portal)
    ///   • Casilla de combate a mitad de cada brazo
    ///
    ///   Disposición visual:
    ///
    ///       [Agua]---ring---[Fuego]
    ///         \               /
    ///        ring    [C]    ring
    ///         /               \
    ///      [Tierra]--ring---[Aire]
    ///
    /// </summary>
    public class BoardGenerator : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject tilePrefab;
        public GameObject centerPrefab;
        [Tooltip("0=Water 1=Fire 2=Earth 3=Air")]
        public GameObject[] castlePrefabs;

        [Header("Layout")]
        [Range(4, 10)] public int tilesPerArm = 6;   // tiles desde castillo al anillo
        [Range(2, 8)] public int ringSegment = 4;   // tiles por lado del anillo entre brazos
        public float armSpacing = 1.5f;              // distancia entre tiles del brazo
        public float tileWidth = 1.0f;
        public float tileHeight = 0.22f;

        [Header("Visual")]
        public bool enableEmission = true;

        // ── Runtime data ──────────────────────────────────────────────────────
        /// <summary>Brazos: paths[0..3] desde castillo hacia el anillo (índice 0 = castillo).</summary>
        public List<Tile>[] paths { get; private set; }
        public Tile centerTile { get; private set; }
        public Tile[] startTiles { get; private set; }

        // Esquinas del anillo (donde se juntan los brazos)
        private Tile[] _corners = new Tile[4];

        // Tiles del anillo entre esquinas: _ring[i] = lista de tiles entre corner i y corner (i+1)%4
        private List<Tile>[] _ringSegments;

        // Direcciones diagonales de cada brazo desde el centro
        // 0=Water(top-left) 1=Fire(top-right) 2=Earth(bottom-left) 3=Air(bottom-right)
        private static readonly Vector3[] ArmDirs =
        {
            new Vector3(-1f, 0f,  1f).normalized,
            new Vector3( 1f, 0f,  1f).normalized,
            new Vector3(-1f, 0f, -1f).normalized,
            new Vector3( 1f, 0f, -1f).normalized,
        };

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake() => GenerateBoard();

        // ── Public API ────────────────────────────────────────────────────────
        public Tile GetStartTile(int playerIndex)
        {
            int idx = playerIndex % 4;
            if (paths == null || paths[idx] == null || paths[idx].Count == 0)
            {
                Debug.LogError($"[BoardGenerator] GetStartTile({playerIndex}): paths[{idx}] es null o vacío!");
                return null;
            }
            return paths[idx][0];
        }

        /// <summary>Retorna la startTile del brazo que corresponde al elemento dado.</summary>
        public Tile GetStartTileByElement(ElementType element)
        {
            int idx = (int)element;
            if (paths == null || paths[idx] == null || paths[idx].Count == 0)
            {
                Debug.LogError($"[BoardGenerator] GetStartTileByElement({element}): paths[{idx}] null!");
                return null;
            }
            return paths[idx][0];
        }
        public Tile GetTile(int pathIndex, int pos) => paths[pathIndex][pos];

        // ── Generation ────────────────────────────────────────────────────────
        [ContextMenu("Generate Board")]
        public void GenerateBoard()
        {
            ClearBoard();

            paths = new List<Tile>[4];
            startTiles = new Tile[4];
            _ringSegments = new List<Tile>[4];

            // ── 1. Centro ────────────────────────────────────────────────────
            centerTile = SpawnTile(Vector3.zero, ElementType.Center, TileType.Center, -1, -1);
            if (centerPrefab != null)
                Instantiate(centerPrefab, Vector3.zero, Quaternion.identity, transform);

            // ── 2. Brazos diagonales ─────────────────────────────────────────
            // La esquina del anillo está a (tilesPerArm + 1) * armSpacing del centro
            float cornerDist = (tilesPerArm + 1) * armSpacing;

            for (int p = 0; p < 4; p++)
            {
                paths[p] = new List<Tile>();
                Vector3 dir = ArmDirs[p];
                ElementType elem = (ElementType)p;

                // Tiles del brazo: índice 0 = castillo (más lejos), último = esquina del anillo
                // Generamos de afuera hacia adentro para facilitar la navegación
                for (int i = tilesPerArm; i >= 0; i--)
                {
                    float dist = (i + 1) * armSpacing;
                    Vector3 pos = dir * dist;

                    TileType tType = TileType.Normal;
                    if (i == tilesPerArm) tType = TileType.Start;  // castillo
                    else if (i == tilesPerArm / 2) tType = TileType.Combat; // mitad del brazo

                    Tile t = SpawnTile(pos, elem, tType, p, i);
                    paths[p].Add(t);
                }

                // paths[p][0] = castillo, paths[p][last] = tile junto al anillo (esquina interna)
                startTiles[p] = paths[p][0];

                // Esquina del anillo
                Vector3 cornerPos = dir * cornerDist;
                Tile corner = SpawnTile(cornerPos, elem, TileType.Normal, p, -2);
                if (corner == null) { Debug.LogError("[BoardGenerator] corner null — verifica que Tile Prefab esté asignado."); return; }
                corner.name = $"Corner_{elem}";
                _corners[p] = corner;

                // Castillo decorativo
                if (castlePrefabs != null && p < castlePrefabs.Length && castlePrefabs[p] != null)
                {
                    Vector3 castlePos = dir * (cornerDist + armSpacing * 1.8f);
                    var castle = Instantiate(castlePrefabs[p], castlePos, Quaternion.identity, transform);
                    castle.transform.LookAt(Vector3.zero);
                }
            }

            // ── 3. Anillo entre esquinas ──────────────────────────────────────
            // Orden de esquinas para el anillo: Water(0) → Fire(1) → Air(3) → Earth(2) → Water
            int[] ringOrder = { 0, 1, 3, 2 };

            for (int r = 0; r < 4; r++)
            {
                int idxA = ringOrder[r];
                int idxB = ringOrder[(r + 1) % 4];

                Vector3 posA = _corners[idxA].transform.position;
                Vector3 posB = _corners[idxB].transform.position;

                // Elemento: mezcla de los dos elementos de los extremos (usamos el primero)
                ElementType ringElem = (ElementType)idxA;

                _ringSegments[r] = new List<Tile>();
                for (int s = 1; s <= ringSegment; s++)
                {
                    float t = (float)s / (ringSegment + 1);
                    Vector3 pos = Vector3.Lerp(posA, posB, t);
                    pos.y = 0f;

                    Tile rt = SpawnTile(pos, ringElem, TileType.Normal, -1, -1);
                    rt.name = $"Ring_{idxA}to{idxB}_{s}";
                    _ringSegments[r].Add(rt);
                }
            }

            // ── 4. Conectar navegación ────────────────────────────────────────
            WireNavigation(ringOrder);

            Debug.Log($"[BoardGenerator] Tablero generado — {CountTiles()} tiles.");
        }

        // ── Wiring ────────────────────────────────────────────────────────────
        private void WireNavigation(int[] ringOrder)
        {
            // ── Brazos: castillo → ... → último tile → esquina ────────────────
            for (int p = 0; p < 4; p++)
            {
                List<Tile> arm = paths[p];
                for (int i = 0; i < arm.Count - 1; i++)
                {
                    arm[i].nextTile = arm[i + 1];
                    arm[i + 1].previousTile = arm[i];
                }
                // Último tile del brazo apunta a la esquina
                arm[arm.Count - 1].nextTile = _corners[p];
                _corners[p].previousTile = arm[arm.Count - 1];
            }

            // ── Anillo: wire segmentos entre esquinas ─────────────────────────
            for (int r = 0; r < 4; r++)
            {
                int idxA = ringOrder[r];
                int idxB = ringOrder[(r + 1) % 4];
                var seg = _ringSegments[r];
                Tile cornerA = _corners[idxA];
                Tile cornerB = _corners[idxB];

                if (seg.Count == 0) continue;

                // Color split
                int half = seg.Count / 2;
                for (int s = 0; s < seg.Count; s++)
                {
                    seg[s].element = s < half ? (ElementType)idxA : (ElementType)idxB;
                    if (enableEmission) seg[s].ApplyElementVisuals();
                }

                // Wire: cornerA → seg[0] → ... → seg[last] → cornerB
                seg[0].previousTile = cornerA;
                for (int s = 0; s < seg.Count - 1; s++)
                {
                    seg[s].nextTile = seg[s + 1];
                    seg[s + 1].previousTile = seg[s];
                }
                seg[seg.Count - 1].nextTile = cornerB;
            }

            // ── Esquinas: connectedTiles = [hacia centro, anillo izq, anillo der] ──
            // Construye mapa: qué segmento de anillo empieza/termina en cada esquina
            // ringOrder: 0→1→3→2→0
            // seg[r] va de ringOrder[r] a ringOrder[r+1]
            // Para corner idxA: seg que SALE = seg[r donde ringOrder[r]==idxA]
            //                   seg que LLEGA = seg[r donde ringOrder[(r+1)%4]==idxA]

            for (int p = 0; p < 4; p++)
            {
                Tile corner = _corners[p];
                List<Tile> options = new List<Tile>();

                // Opción 1: ir al centro
                options.Add(centerTile);

                // Opción 2: primer tile del segmento de anillo que SALE de esta esquina
                for (int r = 0; r < 4; r++)
                {
                    if (ringOrder[r] == p && _ringSegments[r].Count > 0)
                    {
                        options.Add(_ringSegments[r][0]);
                        break;
                    }
                }

                // Opción 3: último tile del segmento de anillo que LLEGA a esta esquina
                for (int r = 0; r < 4; r++)
                {
                    if (ringOrder[(r + 1) % 4] == p && _ringSegments[r].Count > 0)
                    {
                        options.Add(_ringSegments[r][_ringSegments[r].Count - 1]);
                        break;
                    }
                }

                corner.nextTile = null;           // sin dirección por defecto
                corner.connectedTiles = options.ToArray();
            }

            // ── Centro: sin nextTile, es destino final ────────────────────────
            centerTile.nextTile = null;
            centerTile.connectedTiles = null;
        }

        // ── Spawn helpers ─────────────────────────────────────────────────────
        private Tile SpawnTile(Vector3 pos, ElementType elem,
                               TileType tType, int pathIdx, int posIdx)
        {
            if (tilePrefab == null)
            {
                Debug.LogError("[BoardGenerator] tilePrefab no asignado.");
                return null;
            }

            GameObject go = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
            go.transform.localScale = new Vector3(tileWidth, tileHeight, tileWidth);
            go.name = tType == TileType.Center ? "Tile_Center"
                    : $"Tile_{elem}_P{pathIdx}_I{posIdx}";

            Tile tile = go.GetComponent<Tile>() ?? go.AddComponent<Tile>();
            tile.element = elem;
            tile.tileType = tType;
            tile.pathIndex = pathIdx;
            tile.positionOnPath = posIdx;

            if (enableEmission) tile.ApplyElementVisuals();

            return tile;
        }

        private void ClearBoard()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

        private int CountTiles()
        {
            int n = 1 + 4; // center + 4 corners
            foreach (var p in paths) n += p.Count;
            foreach (var s in _ringSegments) if (s != null) n += s.Count;
            return n;
        }

        // ── Gizmos ────────────────────────────────────────────────────────────
        private void OnDrawGizmos()
        {
            if (paths == null) return;
            Color[] cols = { Color.blue, Color.red, Color.green, Color.white };

            for (int p = 0; p < 4; p++)
            {
                if (paths[p] == null) continue;
                Gizmos.color = cols[p];

                for (int i = 0; i < paths[p].Count - 1; i++)
                    if (paths[p][i] && paths[p][i + 1])
                        Gizmos.DrawLine(paths[p][i].transform.position,
                                        paths[p][i + 1].transform.position);

                if (_corners != null && _corners[p] != null && paths[p].Count > 0)
                    Gizmos.DrawLine(paths[p][paths[p].Count - 1].transform.position,
                                    _corners[p].transform.position);
            }

            if (_ringSegments == null) return;
            Gizmos.color = Color.yellow;
            for (int r = 0; r < 4; r++)
            {
                if (_ringSegments[r] == null) continue;
                for (int s = 0; s < _ringSegments[r].Count - 1; s++)
                    if (_ringSegments[r][s] && _ringSegments[r][s + 1])
                        Gizmos.DrawLine(_ringSegments[r][s].transform.position,
                                        _ringSegments[r][s + 1].transform.position);
            }
        }
    }
}