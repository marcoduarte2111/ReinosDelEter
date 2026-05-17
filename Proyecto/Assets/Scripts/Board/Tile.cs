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

        [Header("Graph")]
        public List<Tile> neighbors = new();

        [Header("Conquest")]
        public int ownedByPlayer = -1;
        public bool IsCastle => tileType == TileType.Start;
        public bool IsConquered => IsCastle && ownedByPlayer >= 0;

        [Header("Visuals")]
        public Renderer tileRenderer;

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

        private Material _originalMat;
        private Material _instanceMat;

        private void Awake()
        {
            tileRenderer = GetComponentInChildren<Renderer>();
            if (tileRenderer == null)
                tileRenderer = GetComponent<Renderer>();
            if (tileRenderer != null)
                _originalMat = tileRenderer.sharedMaterial;
        }

        public void AddNeighbor(Tile t)
        {
            if (t != null && !neighbors.Contains(t))
                neighbors.Add(t);
        }

        public void ApplyElementVisuals()
        {
            if (tileRenderer == null) return;
            EnsureInstanceMat();
            Color col = ElementColors[(int)element];
            _instanceMat.color = col;
            if (_instanceMat.HasProperty("_EmissionColor"))
            {
                _instanceMat.EnableKeyword("_EMISSION");
                _instanceMat.SetColor("_EmissionColor", col * 0.3f);
            }
        }

        public void SetHighlight(bool active)
        {
            if (tileRenderer == null) return;
            if (active)
            {
                EnsureInstanceMat();
                if (_instanceMat.HasProperty("_EmissionColor"))
                {
                    _instanceMat.EnableKeyword("_EMISSION");
                    _instanceMat.SetColor("_EmissionColor", Color.yellow * 2f);
                }
                _instanceMat.color = Color.Lerp(_instanceMat.color, Color.yellow, 0.45f);
            }
            else
            {
                RestoreOriginalMaterial();
            }
        }

        public void PlayLandEffect() { }

        public void Conquer(int playerIndex, Color playerColor)
        {
            ownedByPlayer = playerIndex;
            EnsureInstanceMat();
            Color base_ = _originalMat != null ? _originalMat.color : Color.white;
            _instanceMat.color = Color.Lerp(base_, playerColor, 0.45f);
            if (_instanceMat.HasProperty("_EmissionColor"))
            {
                _instanceMat.EnableKeyword("_EMISSION");
                _instanceMat.SetColor("_EmissionColor", playerColor * 0.5f);
            }
        }

        public void Free()
        {
            ownedByPlayer = -1;
            RestoreOriginalMaterial();
        }

        private void EnsureInstanceMat()
        {
            if (tileRenderer == null) return;
            if (_instanceMat == null)
            {
                Material source = _originalMat != null ? _originalMat : tileRenderer.sharedMaterial;
                _instanceMat = new Material(source);
                tileRenderer.material = _instanceMat;
            }
        }

        private void RestoreOriginalMaterial()
        {
            if (tileRenderer == null) return;
            if (_originalMat != null)
                tileRenderer.material = _originalMat;
            _instanceMat = null;
        }
    }
}