using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ReinosDelEter
{
    /// <summary>
    /// HUDController — maneja todo el UI del juego.
    ///
    /// ESTRUCTURA DEL CANVAS (créala manualmente o usa el prefab):
    ///
    ///   Canvas
    ///   ├── TopBar
    ///   │   ├── PlayerPanel_0..3  (nombre, elemento, HP, energía)
    ///   │   └── TurnLabel
    ///   ├── BottomBar
    ///   │   ├── HandContainer     (8 slots de carta)
    ///   │   ├── DiceDisplay       (número del dado)
    ///   │   └── MessageLabel
    ///   ├── CombatPanel           (oculto por defecto)
    ///   │   ├── AttackerArea
    ///   │   ├── VSLabel
    ///   │   └── DefenderArea
    ///   └── EventLog
    ///
    /// SETUP RÁPIDO SIN PREFAB:
    ///   Adjunta este script a cualquier Canvas vacío y llama SetupMinimalHUD()
    ///   desde el Inspector (ContextMenu) para que genere los elementos automáticamente.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Referencias UI — déjalas vacías para auto-generar")]
        public TextMeshProUGUI turnLabel;
        public TextMeshProUGUI messageLabel;
        public TextMeshProUGUI diceLabel;
        public TextMeshProUGUI eventLogLabel;
        public Button rollDiceButton;

        [Header("Paneles de jugador (TopBar)")]
        public PlayerHUDPanel[] playerPanels;   // 4 elementos

        [Header("Panel de combate")]
        public GameObject combatPanel;
        public RawImage attackerCardDisplay;
        public RawImage defenderCardDisplay;
        public TextMeshProUGUI combatResultLabel;
        public Animator combatAnimator;     // opcional

        [Header("Mano de cartas (BottomBar)")]
        public Transform handContainer;      // HorizontalLayoutGroup
        public GameObject cardSlotPrefab;     // prefab de slot de carta

        [Header("Placeholder settings")]
        public Texture2D placeholderTexture;    // null = se genera en código

        // ── Runtime ──────────────────────────────────────────────────────────
        private List<PlayerData> _players;
        private List<CardSlotUI> _handSlots = new();
        private List<string> _log = new();
        private const int MaxLog = 5;

        // ── Inicialización ───────────────────────────────────────────────────
        public void Initialize(List<PlayerData> players)
        {
            _players = players;

            // Auto-detectar handContainer si no está asignado
            if (handContainer == null)
            {
                Transform hc = transform.Find("BottomBar/HandContainer");
                if (hc != null)
                {
                    handContainer = hc;
                    Debug.Log("[HUD] handContainer auto-detectado");
                }
                else
                {
                    Debug.LogWarning("[HUD] handContainer NO ENCONTRADO. Creando automáticamente...");
                    CreateHandContainerIfNeeded();
                }
            }

            // Si no hay referencias asignadas, auto-genera un HUD mínimo
            if (turnLabel == null) SetupMinimalHUD();

            // Botón dado
            if (rollDiceButton != null)
                rollDiceButton.onClick.AddListener(() => GameManager.Instance.OnRollDice());

            // Panel de combate oculto al inicio
            if (combatPanel != null) combatPanel.SetActive(false);

            // Inicializa paneles de jugadores
            for (int i = 0; i < players.Count && i < playerPanels?.Length; i++)
                playerPanels[i]?.Setup(players[i]);

            // DEBUG: Mostrar estado de referencias críticas
            DebugReferences();

            ShowTurn(players[0]);
        }

        /// <summary>Crea HandContainer si no existe.</summary>
        private void CreateHandContainerIfNeeded()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            Transform bottomBar = canvas.transform.Find("BottomBar");
            if (bottomBar == null)
            {
                GameObject bottomBarGO = new GameObject("BottomBar");
                bottomBar = bottomBarGO.transform;
                bottomBar.SetParent(canvas.transform, false);
                var rt = bottomBarGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 0.15f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            Transform hc = bottomBar.Find("HandContainer");
            if (hc == null)
            {
                GameObject hcGO = new GameObject("HandContainer");
                hc = hcGO.transform;
                hc.SetParent(bottomBar, false);
                
                var hcrt = hcGO.AddComponent<RectTransform>();
                hcrt.anchorMin = Vector2.zero;
                hcrt.anchorMax = Vector2.one;
                hcrt.offsetMin = Vector2.zero;
                hcrt.offsetMax = Vector2.zero;
                
                var hlg = hcGO.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 5;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
            }

            handContainer = hc;
            Debug.Log("[HUD] handContainer creado automáticamente");
        }

        /// <summary>Muestra en consola qué referencias faltan.</summary>
        private void DebugReferences()
        {
            string status = "[HUD] Estado de referencias:\n";
            status += $"  • handContainer: {(handContainer != null ? "✓" : "✗ NULL")}\n";
            status += $"  • cardSlotPrefab: {(cardSlotPrefab != null ? "✓" : "✗ NULL (usando fallback)")}\n";
            status += $"  • turnLabel: {(turnLabel != null ? "✓" : "✗ NULL")}\n";
            status += $"  • messageLabel: {(messageLabel != null ? "✓" : "✗ NULL")}\n";
            
            Debug.Log(status);
        }

        // ── API pública ──────────────────────────────────────────────────────

        public void ShowTurn(PlayerData pd)
        {
            if (turnLabel != null)
                turnLabel.text = $"Turno — {pd.playerName}  [{pd.ElementName}]";

            // Resalta el panel del jugador activo
            if (playerPanels != null)
                for (int i = 0; i < playerPanels.Length; i++)
                    playerPanels[i]?.SetActive(i == pd.index);

            // Actualiza la mano mostrada
            RefreshHand(pd);
        }

        public void ShowDiceRoll(int value)
        {
            if (diceLabel != null) diceLabel.text = value.ToString();
        }

        public void SetMessage(string msg)
        {
            if (messageLabel != null) messageLabel.text = msg;
        }

        public void UpdatePlayerStats(PlayerData pd)
        {
            if (playerPanels != null && pd.index < playerPanels.Length)
                playerPanels[pd.index]?.Refresh(pd);
        }

        public void ShowCombatPanel(PlayerData attacker, PlayerData defender)
        {
            if (combatPanel == null) return;
            combatPanel.SetActive(true);

            // Muestra cartas placeholder de cada jugador
            if (attackerCardDisplay != null)
                attackerCardDisplay.texture = GetCardTexture(attacker.hand.Count > 0 ? attacker.hand[0] : null);
            if (defenderCardDisplay != null)
                defenderCardDisplay.texture = GetCardTexture(defender.hand.Count > 0 ? defender.hand[0] : null);

            if (combatResultLabel != null)
                combatResultLabel.text = $"{attacker.playerName}  VS  {defender.playerName}";

            if (combatAnimator != null)
                combatAnimator.SetTrigger("StartCombat");
        }

        public void HideCombatPanel()
        {
            if (combatPanel != null) combatPanel.SetActive(false);
        }

        public void AddToLog(string msg)
        {
            _log.Add(msg);
            if (_log.Count > MaxLog) _log.RemoveAt(0);
            if (eventLogLabel != null)
                eventLogLabel.text = string.Join("\n", _log);
        }

        // ── Mano de cartas ───────────────────────────────────────────────────
        private void RefreshHand(PlayerData pd)
        {
            if (pd == null || pd.hand == null)
            {
                Debug.LogWarning("[HUD] PlayerData o hand es NULL");
                return;
            }

            Debug.Log($"[HUD] Refrescando mano: {pd.playerName} ({pd.hand.Count} cartas)");

            // Verificar handContainer
            if (handContainer == null)
            {
                Debug.LogError("[HUD] handContainer es NULL. No se puede mostrar cartas.");
                return;
            }

            // Limpia slots anteriores
            foreach (var slot in _handSlots) 
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            _handSlots.Clear();

            // Si no hay prefab, crear slots en código (fallback)
            if (cardSlotPrefab == null)
            {
                Debug.Log("[HUD] cardSlotPrefab NULL → usando fallback (creando en código)");
                CreateHandSlotsInCode(pd);
                return;
            }

            // Si hay prefab, usarlo
            foreach (CardData card in pd.hand)
            {
                GameObject go = Instantiate(cardSlotPrefab, handContainer);
                go.name = $"Slot_{card.cardName}";
                CardSlotUI slot = go.GetComponent<CardSlotUI>() ?? go.AddComponent<CardSlotUI>();
                slot.Setup(card, pd, OnCardClicked);
                _handSlots.Add(slot);
            }
        }

        /// <summary>
        /// Fallback: crea slots de carta directamente en código si no hay prefab.
        /// </summary>
        private void CreateHandSlotsInCode(PlayerData pd)
        {
            if (pd.hand.Count == 0)
            {
                Debug.LogWarning("[HUD] Mano vacía");
                return;
            }

            Debug.Log($"[HUD] Creando {pd.hand.Count} slots en código...");

            foreach (CardData card in pd.hand)
            {
                GameObject go = new GameObject($"CardSlot_{card.cardName}");
                go.transform.SetParent(handContainer, false);
                
                // ── RectTransform ──
                var rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(90, 130);
                rt.anchoredPosition = Vector2.zero;
                
                // ── Fondo (Background Image) ──
                var bgImage = go.AddComponent<Image>();
                bgImage.color = Color.gray;
                bgImage.raycastTarget = true;

                // ── Crear Art Image (para mostrar sprite) ──
                GameObject artGo = new GameObject("Art");
                artGo.transform.SetParent(go.transform, false);
                var artRT = artGo.AddComponent<RectTransform>();
                artRT.anchorMin = new Vector2(0.05f, 0.3f);
                artRT.anchorMax = new Vector2(0.95f, 0.95f);
                artRT.offsetMin = Vector2.zero;
                artRT.offsetMax = Vector2.zero;
                var artImage = artGo.AddComponent<Image>();
                artImage.raycastTarget = false;
                artImage.preserveAspect = true;
                artImage.material = null;
                // IMPORTANTE: asegurar que sea visible
                artImage.enabled = true;

                // ── Crear Placeholder RawImage ──
                GameObject phGo = new GameObject("Placeholder");
                phGo.transform.SetParent(go.transform, false);
                var phRT = phGo.AddComponent<RectTransform>();
                phRT.anchorMin = new Vector2(0.05f, 0.3f);
                phRT.anchorMax = new Vector2(0.95f, 0.95f);
                phRT.offsetMin = Vector2.zero;
                phRT.offsetMax = Vector2.zero;
                var phImage = phGo.AddComponent<RawImage>();
                phImage.raycastTarget = false;

                // ── Labels de texto ──
                // Nombre
                GameObject nameGo = new GameObject("Name");
                nameGo.transform.SetParent(go.transform, false);
                var nameRT = nameGo.AddComponent<RectTransform>();
                nameRT.anchorMin = new Vector2(0.0f, 0.82f);
                nameRT.anchorMax = new Vector2(1f, 1f);
                nameRT.offsetMin = Vector2.zero;
                nameRT.offsetMax = Vector2.zero;
                var nameTMP = nameGo.AddComponent<TextMeshProUGUI>();
                nameTMP.fontSize = 9;
                nameTMP.alignment = TextAlignmentOptions.Center;
                nameTMP.color = Color.white;
                nameTMP.fontStyle = FontStyles.Bold;
                nameTMP.text = card.cardName;

                // ATK
                GameObject atkGo = new GameObject("ATK");
                atkGo.transform.SetParent(go.transform, false);
                var atkRT = atkGo.AddComponent<RectTransform>();
                atkRT.anchorMin = new Vector2(0f, 0.15f);
                atkRT.anchorMax = new Vector2(0.5f, 0.3f);
                atkRT.offsetMin = Vector2.zero;
                atkRT.offsetMax = Vector2.zero;
                var atkTMP = atkGo.AddComponent<TextMeshProUGUI>();
                atkTMP.fontSize = 9;
                atkTMP.alignment = TextAlignmentOptions.Center;
                atkTMP.color = Color.white;
                atkTMP.text = $"ATK {card.attackPower}";

                // DEF
                GameObject defGo = new GameObject("DEF");
                defGo.transform.SetParent(go.transform, false);
                var defRT = defGo.AddComponent<RectTransform>();
                defRT.anchorMin = new Vector2(0.5f, 0.15f);
                defRT.anchorMax = new Vector2(1f, 0.3f);
                defRT.offsetMin = Vector2.zero;
                defRT.offsetMax = Vector2.zero;
                var defTMP = defGo.AddComponent<TextMeshProUGUI>();
                defTMP.fontSize = 9;
                defTMP.alignment = TextAlignmentOptions.Center;
                defTMP.color = Color.white;
                defTMP.text = $"DEF {card.defensePower}";

                // Cost
                GameObject costGo = new GameObject("Cost");
                costGo.transform.SetParent(go.transform, false);
                var costRT = costGo.AddComponent<RectTransform>();
                costRT.anchorMin = new Vector2(0.65f, 0f);
                costRT.anchorMax = new Vector2(1f, 0.18f);
                costRT.offsetMin = Vector2.zero;
                costRT.offsetMax = Vector2.zero;
                var costTMP = costGo.AddComponent<TextMeshProUGUI>();
                costTMP.fontSize = 10;
                costTMP.alignment = TextAlignmentOptions.Center;
                costTMP.color = Color.white;
                costTMP.fontStyle = FontStyles.Bold;
                costTMP.text = $"Cost {card.energyCost}";

                // ── Crear CardSlotUI y asignar componentes ──
                CardSlotUI slot = go.AddComponent<CardSlotUI>();
                slot.cardBackground = bgImage;
                slot.cardArtImage = artImage;
                slot.placeholderImage = phImage;
                slot.cardNameLabel = nameTMP;
                slot.atkLabel = atkTMP;
                slot.defLabel = defTMP;
                slot.costLabel = costTMP;

                // ── Setup sin BuildUIIfNeeded ──
                slot.SetupDirect(card, pd, OnCardClicked);
                _handSlots.Add(slot);
                
                Debug.Log($"  ✓ Slot creado: {card.cardName}");
            }
        }

        private void OnCardClicked(CardData card, PlayerData owner)
        {
            // Durante combate el CombatManager maneja esto;
            // fuera de combate, se ignora (sin energía que gastar)
            CombatManager cm = Object.FindFirstObjectByType<CombatManager>();
            cm?.OnCardSelectedByPlayer(card, owner);
        }

        // ── Placeholder de carta ─────────────────────────────────────────────
        /// <summary>
        /// Retorna la textura de una carta.
        /// Si la carta tiene sprite propio lo convierte; si no, genera un color sólido.
        ///
        /// PARA AÑADIR TU ARTE:
        ///   Asigna cardData.cardArt en el ScriptableObject — este método lo detecta automáticamente.
        /// </summary>
        public Texture2D GetCardTexture(CardData card)
        {
            if (card == null) return MakeSolidTexture(Color.gray, 128, 180);

            // Si la carta tiene sprite asignado, úsalo
            if (card.HasArt && card.cardArt.texture != null)
                return card.cardArt.texture;

            // Placeholder: color sólido del elemento con borde oscuro
            return MakePlaceholderCard(card.PlaceholderColor, card.cardName,
                                       card.attackPower, card.defensePower);
        }

        private Texture2D MakePlaceholderCard(Color baseColor, string name, int atk, int def)
        {
            int w = 128, h = 180;
            Texture2D tex = new Texture2D(w, h);

            Color border = baseColor * 0.4f;
            border.a = 1f;
            Color dark = baseColor * 0.6f; dark.a = 1f;
            Color light = baseColor * 1.1f; light.a = 1f;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = x < 4 || x > w - 5 || y < 4 || y > h - 5;
                    float grad = (float)y / h;
                    Color c = Color.Lerp(light, dark, grad);
                    tex.SetPixel(x, y, isBorder ? border : c);
                }

            tex.Apply();
            return tex;
        }

        private Texture2D MakeSolidTexture(Color color, int w, int h)
        {
            Texture2D tex = new Texture2D(w, h);
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // ── Auto-generación de HUD mínimo ────────────────────────────────────
        /// <summary>
        /// Crea en código un HUD básico funcional si no tienes Canvas prefab.
        /// Útil para la entrega. Crea el Canvas desde Editor y adjunta HUDController
        /// sin asignar nada — este método hace el resto.
        /// </summary>
        [ContextMenu("Setup Minimal HUD")]
        public void SetupMinimalHUD()
        {
            // Limpia hijos anteriores si ya existían
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            if (GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // ── Top bar: turno ───────────────────────────────────────────────
            // Ancla: top-center
            turnLabel = MakeAnchored<TextMeshProUGUI>("TurnLabel",
                new Vector2(0.2f, 0.93f), new Vector2(0.8f, 1f));
            turnLabel.text = "Turno —";
            turnLabel.fontSize = 28;
            turnLabel.alignment = TextAlignmentOptions.Center;
            turnLabel.color = Color.white;

            // ── Top-left: event log ──────────────────────────────────────────
            eventLogLabel = MakeAnchored<TextMeshProUGUI>("EventLog",
                new Vector2(0f, 0.72f), new Vector2(0.25f, 0.93f));
            eventLogLabel.text = "";
            eventLogLabel.fontSize = 16;
            eventLogLabel.alignment = TextAlignmentOptions.TopLeft;
            eventLogLabel.color = Color.white;

            // ── Bottom bar panel ─────────────────────────────────────────────
            var bottomBar = MakeAnchoredGO("BottomBar",
                new Vector2(0f, 0f), new Vector2(1f, 0.18f));
            var bbImg = bottomBar.AddComponent<Image>();
            bbImg.color = new Color(0.05f, 0.05f, 0.08f, 0.88f);

            // Dado (bottom-right)
            var diceGo = MakeAnchoredGO("DiceArea",
                new Vector2(0.82f, 0f), new Vector2(1f, 1f), bottomBar.transform);
            diceGo.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.18f, 1f);
            diceLabel = MakeAnchored<TextMeshProUGUI>("DiceValue",
                Vector2.zero, Vector2.one, diceGo.transform);
            diceLabel.text = "?";
            diceLabel.fontSize = 52;
            diceLabel.alignment = TextAlignmentOptions.Center;
            diceLabel.color = Color.white;

            // Botón lanzar dado (bottom center-right)
            var btnGo = MakeAnchoredGO("RollButton",
                new Vector2(0.62f, 0.15f), new Vector2(0.81f, 0.85f), bottomBar.transform);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.3f, 0.18f, 0.05f, 1f);
            rollDiceButton = btnGo.AddComponent<Button>();
            rollDiceButton.targetGraphic = btnImg;
            rollDiceButton.onClick.AddListener(() => GameManager.Instance?.OnRollDice());
            var btnLabel = MakeAnchored<TextMeshProUGUI>("Label",
                Vector2.zero, Vector2.one, btnGo.transform);
            btnLabel.text = "Lanzar Dado";
            btnLabel.fontSize = 20;
            btnLabel.alignment = TextAlignmentOptions.Center;
            btnLabel.color = Color.white;

            // Mensaje (bottom center-left)
            messageLabel = MakeAnchored<TextMeshProUGUI>("MessageLabel",
                new Vector2(0.01f, 0.55f), new Vector2(0.61f, 1f), bottomBar.transform);
            messageLabel.text = "Esperando...";
            messageLabel.fontSize = 18;
            messageLabel.alignment = TextAlignmentOptions.MidlineLeft;
            messageLabel.color = new Color(0.9f, 0.85f, 0.6f, 1f);

            // Área de cartas (bottom-left zone)
            var handGo = MakeAnchoredGO("HandArea",
                new Vector2(0f, 0.02f), new Vector2(0.61f, 0.52f), bottomBar.transform);
            handGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.6f);
            var handLbl = MakeAnchored<TextMeshProUGUI>("HandLabel",
                Vector2.zero, Vector2.one, handGo.transform);
            handLbl.text = "Tus cartas";
            handLbl.fontSize = 14;
            handLbl.alignment = TextAlignmentOptions.Center;
            handLbl.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            handContainer = handGo.transform;

            // ── Combat panel (center screen) ─────────────────────────────────
            var cpGo = MakeAnchoredGO("CombatPanel",
                new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.85f));
            cpGo.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.1f, 0.95f);
            combatPanel = cpGo;
            combatPanel.SetActive(false);

            // VS label
            combatResultLabel = MakeAnchored<TextMeshProUGUI>("CombatResult",
                new Vector2(0f, 0.8f), new Vector2(1f, 1f), cpGo.transform);
            combatResultLabel.text = "COMBATE";
            combatResultLabel.fontSize = 28;
            combatResultLabel.alignment = TextAlignmentOptions.Center;
            combatResultLabel.color = Color.white;

            // Attacker card (left)
            var atkGo = MakeAnchoredGO("AttackerCard",
                new Vector2(0.05f, 0.1f), new Vector2(0.42f, 0.78f), cpGo.transform);
            atkGo.AddComponent<Image>().color = new Color(0.2f, 0.3f, 0.5f, 1f);
            attackerCardDisplay = atkGo.AddComponent<RawImage>();
            attackerCardDisplay.color = Color.white;

            // VS divider
            var vsLbl = MakeAnchored<TextMeshProUGUI>("VS",
                new Vector2(0.42f, 0.35f), new Vector2(0.58f, 0.65f), cpGo.transform);
            vsLbl.text = "VS";
            vsLbl.fontSize = 32;
            vsLbl.fontStyle = FontStyles.Bold;
            vsLbl.alignment = TextAlignmentOptions.Center;
            vsLbl.color = new Color(1f, 0.6f, 0.1f, 1f);

            // Defender card (right)
            var defGo = MakeAnchoredGO("DefenderCard",
                new Vector2(0.58f, 0.1f), new Vector2(0.95f, 0.78f), cpGo.transform);
            defGo.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 1f);
            defenderCardDisplay = defGo.AddComponent<RawImage>();
            defenderCardDisplay.color = Color.white;

            Debug.Log("[HUDController] HUD generado con anchors. Listo.");
        }

        // ── Anchor builders ──────────────────────────────────────────────────
        private T MakeAnchored<T>(string name, Vector2 anchorMin, Vector2 anchorMax,
            Transform parent = null) where T : Component
        {
            var go = MakeAnchoredGO(name, anchorMin, anchorMax, parent);
            return go.AddComponent<T>();
        }

        private GameObject MakeAnchoredGO(string name, Vector2 anchorMin, Vector2 anchorMax,
            Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent ?? transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

    }

}