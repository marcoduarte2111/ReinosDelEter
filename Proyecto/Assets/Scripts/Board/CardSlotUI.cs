using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

namespace ReinosDelEter
{
    /// <summary>
    /// CardSlotUI — slot de carta en la mano del jugador.
    ///
    /// Muestra:
    ///   • Arte de la carta si CardData.cardArt != null
    ///   • Placeholder de color si no hay arte
    ///   • Nombre, ATK, DEF, costo de energía
    ///   • Hover: escala up
    ///   • Click: llama onCardClicked
    ///
    /// PARA AÑADIR TU ARTE:
    ///   En el ScriptableObject CardData, arrastra tu Sprite al campo "cardArt".
    ///   Este componente lo detecta y lo muestra automáticamente.
    /// </summary>
    public class CardSlotUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Referencias (se crean automáticamente si son null)")]
        public Image cardBackground;
        public Image cardArtImage;       // Sprite art de la carta
        public RawImage placeholderImage;   // Placeholder procedural
        public TextMeshProUGUI cardNameLabel;
        public TextMeshProUGUI atkLabel;
        public TextMeshProUGUI defLabel;
        public TextMeshProUGUI costLabel;

        // Runtime
        private CardData _card;
        private PlayerData _owner;
        private Action<CardData, PlayerData> _onClick;
        private Vector3 _baseScale;

        // Public accessors
        public CardData Card => _card;
        public PlayerData Owner => _owner;

        // ── Setup ─────────────────────────────────────────────────────────────
        public void Setup(CardData card, PlayerData owner, Action<CardData, PlayerData> onClick)
        {
            _card = card;
            _owner = owner;
            _onClick = onClick;
            _baseScale = transform.localScale;

            BuildUIIfNeeded();
            Render();
        }

        /// <summary>
        /// Setup directo sin BuildUIIfNeeded (para cuando ya está todo construido en código).
        /// </summary>
        public void SetupDirect(CardData card, PlayerData owner, Action<CardData, PlayerData> onClick)
        {
            _card = card;
            _owner = owner;
            _onClick = onClick;
            _baseScale = transform.localScale;

            Render();
        }

        private void Render()
        {
            if (_card == null) 
            {
                Debug.LogWarning("[CardSlot] Card is null, cannot render");
                return;
            }

            Debug.Log($"[CardSlot] Rendering: {_card.cardName}, HasArt: {_card.HasArt}");

            // Fondo con color del elemento
            if (cardBackground != null)
            {
                Color bg = _card.PlaceholderColor * 0.6f;
                bg.a = 0.9f;
                cardBackground.color = bg;
                cardBackground.enabled = true;
            }

            // Arte o placeholder
            if (_card.HasArt && _card.cardArt != null)
            {
                if (cardArtImage != null)
                { 
                    cardArtImage.sprite = _card.cardArt;
                    cardArtImage.gameObject.SetActive(true);
                    cardArtImage.enabled = true;
                    Debug.Log($"  ✓ Sprite asignado: {_card.cardArt.name}");
                }
                if (placeholderImage != null) placeholderImage.gameObject.SetActive(false);
            }
            else
            {
                // Sin arte = mostrar placeholder de color
                if (placeholderImage != null)
                {
                    placeholderImage.gameObject.SetActive(true);
                    var hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
                    placeholderImage.texture = hud != null ? hud.GetCardTexture(_card) : null;
                }
                if (cardArtImage != null) cardArtImage.gameObject.SetActive(false);
                Debug.Log($"  ⊘ Sin sprite, usando placeholder");
            }

            // Labels de texto
            if (cardNameLabel != null) cardNameLabel.text = _card.cardName;
            if (atkLabel != null) atkLabel.text = $"ATK {_card.attackPower}";
            if (defLabel != null) defLabel.text = $"DEF {_card.defensePower}";
            if (costLabel != null) costLabel.text = $"Cost {_card.energyCost}";
        }

        // ── Hover / Click ─────────────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData _)
        {
            StopAllCoroutines();
            StartCoroutine(ScaleTo(_baseScale * 1.15f, 0.1f));
        }

        public void OnPointerExit(PointerEventData _)
        {
            StopAllCoroutines();
            StartCoroutine(ScaleTo(_baseScale, 0.1f));
        }

        public void OnPointerClick(PointerEventData _)
        {
            _onClick?.Invoke(_card, _owner);
            StartCoroutine(ClickFlash());
        }

        private System.Collections.IEnumerator ScaleTo(Vector3 target, float duration)
        {
            Vector3 start = transform.localScale;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(start, target, t / duration);
                yield return null;
            }
            transform.localScale = target;
        }

        private System.Collections.IEnumerator ClickFlash()
        {
            Vector3 big = _baseScale * 1.25f;
            for (float t = 0; t < 0.08f; t += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(_baseScale, big, t / 0.08f);
                yield return null;
            }
            for (float t = 0; t < 0.1f; t += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(big, _baseScale, t / 0.1f);
                yield return null;
            }
            transform.localScale = _baseScale;
        }

        // ── Auto-build UI ─────────────────────────────────────────────────────
        /// <summary>
        /// Si no hay prefab de slot asignado, construye los elementos de UI en código.
        /// Esto permite que funcione sin prefabs.
        /// </summary>
        private void BuildUIIfNeeded()
        {
            if (cardBackground != null) return; // ya está configurado

            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(90, 130);

            // Fondo
            cardBackground = gameObject.AddComponent<Image>();
            cardBackground.color = Color.gray;

            // Placeholder RawImage
            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(transform, false);
            placeholderImage = phGo.AddComponent<RawImage>();
            placeholderImage.rectTransform.anchorMin = new Vector2(0.05f, 0.3f);
            placeholderImage.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            placeholderImage.rectTransform.sizeDelta = Vector2.zero;

            // Art Image (encima del placeholder)
            var artGo = new GameObject("Art");
            artGo.transform.SetParent(transform, false);
            cardArtImage = artGo.AddComponent<Image>();
            cardArtImage.rectTransform.anchorMin = new Vector2(0.05f, 0.3f);
            cardArtImage.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            cardArtImage.rectTransform.sizeDelta = Vector2.zero;
            cardArtImage.preserveAspect = true;

            // Nombre
            cardNameLabel = MakeTMP("Name", new Vector2(0.0f, 0.82f), new Vector2(1f, 1f), 9, true);

            // ATK
            atkLabel = MakeTMP("ATK", new Vector2(0f, 0.15f), new Vector2(0.5f, 0.3f), 9);

            // DEF
            defLabel = MakeTMP("DEF", new Vector2(0.5f, 0.15f), new Vector2(1f, 0.3f), 9);

            // Costo
            costLabel = MakeTMP("Cost", new Vector2(0.65f, 0f), new Vector2(1f, 0.18f), 10, true);
        }

        private TextMeshProUGUI MakeTMP(string name,
            Vector2 anchorMin, Vector2 anchorMax,
            float size, bool bold = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            var rt = tmp.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;
            return tmp;
        }
    }
}