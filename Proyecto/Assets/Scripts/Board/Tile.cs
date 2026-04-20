using UnityEngine;

namespace ReinosDelEter
{
    public enum ElementType { Water, Fire, Earth, Air, Center }
    public enum TileType { Normal, Start, Combat, Event, Center }

    public class Tile : MonoBehaviour
    {
        [Header("Tile Identity")]
        public int tileIndex;
        public ElementType element;
        public TileType tileType;

        [Header("Board Position")]
        public int pathIndex;
        public int positionOnPath;

        [Header("Navigation")]
        public Tile nextTile;
        public Tile previousTile;
        public Tile[] connectedTiles;

        [Header("Visuals")]
        public Renderer tileRenderer;

        public static readonly Color[] ElementColors =
        {
            new Color(0.2f,  0.5f,  1.0f),    // Water  — blue
            new Color(1.0f,  0.25f, 0.05f),   // Fire   — orange-red
            new Color(0.15f, 0.65f, 0.15f),   // Earth  — green
            new Color(0.85f, 0.85f, 0.92f),   // Air    — pearl white
            new Color(0.5f,  0.2f,  0.9f)     // Center — purple
        };

        private void Awake()
        {
            tileRenderer = GetComponent<Renderer>();
        }

        public void ApplyElementVisuals()
        {
            if (tileRenderer == null)
                tileRenderer = GetComponent<Renderer>();
            if (tileRenderer == null) return;

            Color col = ElementColors[(int)element];

            // Usa el shader del material existente en lugar de buscarlo por nombre
            Material mat = new Material(tileRenderer.sharedMaterial != null
                ? tileRenderer.sharedMaterial
                : tileRenderer.material);

            mat.color = col;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", col * 0.3f);
            }
            tileRenderer.material = mat;
        }

        public void SetHighlight(bool active)
        {
            if (tileRenderer == null) return;
            Color col = ElementColors[(int)element];
            Material mat = tileRenderer.material;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", active ? Color.yellow * 0.8f : col * 0.3f);
        }

        public void PlayLandEffect() => StartCoroutine(LandPulse());

        private System.Collections.IEnumerator LandPulse()
        {
            if (tileRenderer == null) yield break;
            Color col = ElementColors[(int)element];
            Material mat = tileRenderer.material;
            Color baseEmit = col * 0.3f;
            Color peak = Color.white * 1.2f;
            mat.EnableKeyword("_EMISSION");

            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                float n = t < 0.15f ? t / 0.15f : (0.3f - t) / 0.15f;
                mat.SetColor("_EmissionColor", Color.Lerp(baseEmit, peak, n));
                yield return null;
            }
            mat.SetColor("_EmissionColor", baseEmit);
        }
    }
}