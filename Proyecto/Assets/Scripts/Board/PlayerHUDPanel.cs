using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ReinosDelEter
{
    /// <summary>
    /// PlayerHUDPanel — panel de stats de un jugador en el TopBar.
    ///
    /// Adjunta a un panel de UI que tenga:
    ///   - nameLabel (TMP)      → nombre del jugador
    ///   - elementLabel (TMP)   → emoji/nombre del elemento
    ///   - hpLabel (TMP)        → "❤ 20/20"
    ///   - energyLabel (TMP)    → "⚡ 3/3"
    ///   - background (Image)   → se colorea con el elemento
    ///   - activeBorder (Image) → borde que se enciende en el turno activo
    /// </summary>
    public class PlayerHUDPanel : MonoBehaviour
    {
        [Header("Referencias UI")]
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI elementLabel;
        public TextMeshProUGUI hpLabel;
        public TextMeshProUGUI energyLabel;
        public Image background;
        public Image activeBorder;

        private PlayerData _data;

        // ── Setup ─────────────────────────────────────────────────────────────
        public void Setup(PlayerData pd)
        {
            _data = pd;

            if (background != null)
            {
                Color bg = pd.ElementColor;
                bg.a = 0.75f;
                background.color = bg;
            }

            if (activeBorder != null) activeBorder.gameObject.SetActive(false);

            Refresh(pd);
        }

        public void Refresh(PlayerData pd)
        {
            _data = pd;
            if (nameLabel != null) nameLabel.text = pd.playerName;
            if (elementLabel != null) elementLabel.text = ElementEmoji(pd.element);
            if (hpLabel != null) hpLabel.text = $"❤ {pd.health}/{pd.maxHealth}";
            if (energyLabel != null) energyLabel.text = $"⚡ {pd.energy}/{pd.maxEnergy}";
        }

        public void SetActive(bool isActive)
        {
            if (activeBorder == null) return;
            activeBorder.gameObject.SetActive(isActive);

            // Pulso visual en turno activo
            if (isActive) StartCoroutine(PulseBorder());
        }

        private System.Collections.IEnumerator PulseBorder()
        {
            if (activeBorder == null) yield break;
            float t = 0f;
            Color baseColor = activeBorder.color;

            while (activeBorder.gameObject.activeSelf)
            {
                t += Time.deltaTime * 2f;
                float a = 0.5f + Mathf.Sin(t) * 0.4f;
                activeBorder.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }
        }

        private string ElementEmoji(ElementType el) => el switch
        {
            ElementType.Water => "💧 Agua",
            ElementType.Fire => "🔥 Fuego",
            ElementType.Earth => "🌿 Tierra",
            ElementType.Air => "🌫️ Aire",
            _ => "✦ Centro"
        };
    }
}