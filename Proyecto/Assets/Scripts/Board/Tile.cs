using System.Collections.Generic;
using UnityEngine;

namespace ReinosDelEter
{
    public enum ElementType { Water, Fire, Earth, Air, Center }
    public enum TileType { Normal, Start, Combat, Event, Center }

    public class Tile : MonoBehaviour
    {
        [Header("Identity")]
        public int tileIndex;
        public ElementType element;
        public TileType tileType;
        public int pathIndex;
        public int positionOnPath;

        [Header("Graph — vecinos directos")]
        public List<Tile> neighbors = new();

        [Header("Conquest")]
        public int ownedByPlayer = -1;
        public bool IsCastle => tileType == TileType.Start;
        public bool IsConquered => IsCastle && ownedByPlayer >= 0;

        [Header("Visuals")]
        public Renderer tileRenderer;

        // Kept for legacy compatibility — not used for navigation anymore
        public Tile nextTile { get; set; }
        public Tile previousTile { get; set; }
        public Tile[] connectedTiles { get; set; }

        public static readonly Color[] ElementColors =
        {
            new Color(0.2f,  0.5f,  1.0f),
            new Color(1.0f,  0.25f, 0.05f),
            new Color(0.15f, 0.65f, 0.15f),
            new Color(0.85f, 0.85f, 0.92f),
            new Color(0.5f,  0.2f,  0.9f)
        };

        private Material _mat;

        private void Awake()
        {
            tileRenderer = GetComponent<Renderer>();
        }

        public void AddNeighbor(Tile t)
        {
            if (t != null && !neighbors.Contains(t))
                neighbors.Add(t);
        }

        public void ApplyElementVisuals()
        {
            if (tileRenderer == null) tileRenderer = GetComponent<Renderer>();
            if (tileRenderer == null) return;
            Color col = ElementColors[(int)element];
            _mat = new Material(tileRenderer.sharedMaterial != null
                   ? tileRenderer.sharedMaterial : tileRenderer.material);
            _mat.color = col;
            _mat.EnableKeyword("_EMISSION");
            _mat.SetColor("_EmissionColor", col * 0.3f);
            tileRenderer.material = _mat;
        }

        public void SetHighlight(bool active)
        {
            if (tileRenderer == null) return;
            if (_mat == null) _mat = tileRenderer.material;
            _mat.EnableKeyword("_EMISSION");
            if (active)
            {
                _mat.color = Color.yellow;
                _mat.SetColor("_EmissionColor", Color.yellow * 2f);
            }
            else
            {
                Color col = ElementColors[(int)element];
                _mat.color = col;
                _mat.SetColor("_EmissionColor", col * 0.3f);
            }
        }

        public void PlayLandEffect() => StartCoroutine(LandPulse());

        private System.Collections.IEnumerator LandPulse()
        {
            if (tileRenderer == null) yield break;
            if (_mat == null) _mat = tileRenderer.material;
            Color col = ElementColors[(int)element];
            Color baseEmit = col * 0.3f;
            Color peak = Color.white * 1.5f;
            _mat.EnableKeyword("_EMISSION");
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                float n = t < 0.15f ? t / 0.15f : (0.3f - t) / 0.15f;
                _mat.SetColor("_EmissionColor", Color.Lerp(baseEmit, peak, n));
                yield return null;
            }
            _mat.SetColor("_EmissionColor", baseEmit);
        }

        public void Conquer(int playerIndex, Color playerColor)
        {
            ownedByPlayer = playerIndex;
            if (_mat == null && tileRenderer != null) _mat = tileRenderer.material;
            if (_mat == null) return;
            Color col = Color.Lerp(ElementColors[(int)element], playerColor, 0.55f);
            _mat.color = col;
            _mat.EnableKeyword("_EMISSION");
            _mat.SetColor("_EmissionColor", playerColor * 0.7f);
        }

        public void Free()
        {
            ownedByPlayer = -1;
            ApplyElementVisuals();
        }
    }
}