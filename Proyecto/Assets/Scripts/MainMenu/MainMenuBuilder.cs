using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace ReinosDelEter
{
    /// <summary>
    /// MainMenuBuilder — construye todo el menú principal en código.
    /// No necesitas crear nada en el editor excepto:
    ///   1. Empty GameObject con este script
    ///   2. Asignar backgroundSprite (imagen de portada) en el Inspector
    ///
    /// SETUP:
    ///   1. Crea una escena vacía llamada "MainMenu"
    ///   2. Crea un Empty GameObject → Add Component → MainMenuBuilder
    ///   3. Asigna tu imagen de portada al campo backgroundSprite
    ///   4. Dale Play
    /// </summary>
    public class MainMenuBuilder : MonoBehaviour
    {
        [Header("Imagen de fondo (portada)")]
        public Sprite backgroundSprite;

        [Header("Colores del tema")]
        public Color accentColor = new Color(0.33f, 0.29f, 0.72f, 1f); // púrpura
        public Color darkOverlay = new Color(0.06f, 0.04f, 0.12f, 0.82f);
        public Color cardColor = new Color(0.08f, 0.06f, 0.14f, 0.92f);
        public Color textPrimary = new Color(0.95f, 0.93f, 1.0f, 1f);
        public Color textSecondary = new Color(0.65f, 0.62f, 0.80f, 1f);
        public Color inputBg = new Color(0.12f, 0.09f, 0.20f, 1f);
        public Color buttonHover = new Color(0.42f, 0.38f, 0.85f, 1f);

        [Header("Escena de juego")]
        public string gameSceneName = "Game";

        // Elementos de UI
        private TMP_InputField[] _nameInputs = new TMP_InputField[4];
        private Slider _armSlider, _ringSlider;
        private TMP_Text _armVal, _ringVal;
        private GameObject _quickPanel, _customPanel;
        private bool _isCustomMode = false;

        private void Start() => BuildMenu();

        private void BuildMenu()
        {
            // ── Canvas ────────────────────────────────────────────────────────
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Fondo ─────────────────────────────────────────────────────────
            var bg = MakeImage("Background", canvasGO.transform, Color.white);
            StretchFull(bg);
            if (backgroundSprite != null)
                bg.GetComponent<Image>().sprite = backgroundSprite;
            else
                bg.GetComponent<Image>().color = new Color(0.08f, 0.05f, 0.15f, 1f);
            bg.GetComponent<Image>().type = Image.Type.Simple;
            bg.GetComponent<Image>().preserveAspect = false;

            // Overlay oscuro sobre el fondo
            var overlay = MakeImage("Overlay", canvasGO.transform, darkOverlay);
            StretchFull(overlay);

            // ── Panel central ─────────────────────────────────────────────────
            var panel = MakeRect("MenuPanel", canvasGO.transform);
            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(480f, 700f);
            panelRT.anchoredPosition = Vector2.zero;

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = cardColor;
            SetRadius(panelImg, 20f);

            // Layout vertical
            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 14f;
            vlg.padding = new RectOffset(28, 28, 32, 28);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;

            // ── Título ────────────────────────────────────────────────────────
            var title = MakeTMP("Title", panel.transform, "Reinos del Éter", 32f, FontStyles.Bold, textPrimary);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 44f);
            title.alignment = TextAlignmentOptions.Center;

            var subtitle = MakeTMP("Subtitle", panel.transform, "Juego de tablero · 4 jugadores", 14f, FontStyles.Normal, textSecondary);
            subtitle.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 22f);
            subtitle.alignment = TextAlignmentOptions.Center;

            // Separador
            AddSeparator(panel.transform);

            // ── Tabs ──────────────────────────────────────────────────────────
            var tabRow = MakeHRow("TabRow", panel.transform, 44f, 8f);

            Button quickBtn = null, customBtn = null;
            MakeTabButton(tabRow.transform, "Inicio rápido", true, ref quickBtn);
            MakeTabButton(tabRow.transform, "Personalizar", false, ref customBtn);

            // ── Panel inicio rápido ───────────────────────────────────────────
            _quickPanel = MakeRect("QuickPanel", panel.transform);
            _quickPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 56f);
            var quickText = MakeTMP("QuickInfo", _quickPanel.transform,
                "Los jugadores se llamarán Jugador 1, 2, 3 y 4.\nLos elementos se asignan al azar.",
                13f, FontStyles.Normal, textSecondary);
            quickText.alignment = TextAlignmentOptions.Center;
            StretchFull(quickText.gameObject);

            // ── Panel personalizar ────────────────────────────────────────────
            _customPanel = MakeRect("CustomPanel", panel.transform);
            _customPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 180f);
            _customPanel.SetActive(false);

            var inputVLG = _customPanel.AddComponent<VerticalLayoutGroup>();
            inputVLG.spacing = 8f;
            inputVLG.childControlWidth = true;
            inputVLG.childControlHeight = false;
            inputVLG.childForceExpandWidth = true;

            Color[] playerColors = {
                new Color(0.2f, 0.5f, 1.0f, 1f),
                new Color(1.0f, 0.3f, 0.15f, 1f),
                new Color(0.2f, 0.75f, 0.2f, 1f),
                new Color(0.75f, 0.75f, 0.85f, 1f),
            };
            string[] placeholders = { "Jugador 1", "Jugador 2", "Jugador 3", "Jugador 4" };

            for (int i = 0; i < 4; i++)
                _nameInputs[i] = MakePlayerInput(_customPanel.transform, placeholders[i], playerColors[i], i);

            // ── Tablero ───────────────────────────────────────────────────────
            AddSeparator(panel.transform);

            var boardLabel = MakeTMP("BoardLabel", panel.transform, "Configuración del tablero", 12f, FontStyles.Normal, textSecondary);
            boardLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 18f);

            (_armSlider, _armVal) = MakeSliderRow(panel.transform, "Tiles por brazo", 3, 10, 5);
            (_ringSlider, _ringVal) = MakeSliderRow(panel.transform, "Tiles por anillo", 2, 8, 4);

            // ── Botones ───────────────────────────────────────────────────────
            AddSeparator(panel.transform);

            var startBtn = MakeButton("StartBtn", panel.transform, "Iniciar partida", accentColor, textPrimary, 48f);
            startBtn.onClick.AddListener(StartGame);

            var quitBtn = MakeButton("QuitBtn", panel.transform, "Salir del juego",
                new Color(0.2f, 0.15f, 0.3f, 1f), textSecondary, 38f);
            quitBtn.onClick.AddListener(QuitGame);

            // ── Callbacks de tabs ─────────────────────────────────────────────
            quickBtn?.onClick.AddListener(() =>
            {
                _isCustomMode = false;
                _quickPanel.SetActive(true);
                _customPanel.SetActive(false);
                SetTabActive(quickBtn, true);
                SetTabActive(customBtn, false);
            });

            customBtn?.onClick.AddListener(() =>
            {
                _isCustomMode = true;
                _quickPanel.SetActive(false);
                _customPanel.SetActive(true);
                SetTabActive(quickBtn, false);
                SetTabActive(customBtn, true);
            });
        }

        // ── Game actions ──────────────────────────────────────────────────────

        private void StartGame()
        {
            // Asegura GameConfig
            if (GameConfig.Instance == null)
            {
                var go = new GameObject("GameConfig");
                go.AddComponent<GameConfig>();
            }

            string[] names = new string[4];
            for (int i = 0; i < 4; i++)
            {
                string n = _isCustomMode && _nameInputs[i] != null
                    ? _nameInputs[i].text.Trim() : "";
                names[i] = n.Length > 0 ? n : $"Jugador {i + 1}";
            }

            GameConfig.Instance.playerNames = names;
            GameConfig.Instance.tilesPerArm = _armSlider != null ? (int)_armSlider.value : 5;
            GameConfig.Instance.ringTilesPerSide = _ringSlider != null ? (int)_ringSlider.value : 4;

            SceneManager.LoadScene(gameSceneName);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── UI Builders ───────────────────────────────────────────────────────

        private TMP_InputField MakePlayerInput(Transform parent, string placeholder, Color accent, int idx)
        {
            var row = MakeRect($"InputRow_{idx}", parent);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 36f);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childControlHeight = true;
            rowLayout.childControlWidth = false;
            rowLayout.childForceExpandHeight = true;

            // Dot de color
            var dot = MakeRect("Dot", row.transform);
            var dotRT = dot.GetComponent<RectTransform>();
            dotRT.sizeDelta = new Vector2(10f, 10f);
            var dotImg = dot.AddComponent<Image>();
            dotImg.color = accent;
            SetRadius(dotImg, 5f);
            var dotLE = dot.AddComponent<LayoutElement>();
            dotLE.preferredWidth = 10f;
            dotLE.preferredHeight = 10f;
            dotLE.flexibleWidth = 0f;

            // Input
            var inputGO = MakeRect("Input", row.transform);
            var inputRT = inputGO.GetComponent<RectTransform>();
            var inputLE = inputGO.AddComponent<LayoutElement>();
            inputLE.flexibleWidth = 1f;

            var inputImg = inputGO.AddComponent<Image>();
            inputImg.color = inputBg;
            SetRadius(inputImg, 8f);

            var field = inputGO.AddComponent<TMP_InputField>();

            // Text area
            var textArea = MakeRect("Text Area", inputGO.transform);
            StretchFull(textArea);
            var textAreaRT = textArea.GetComponent<RectTransform>();
            textAreaRT.offsetMin = new Vector2(8, 4);
            textAreaRT.offsetMax = new Vector2(-8, -4);
            textArea.AddComponent<RectMask2D>();

            var inputText = MakeTMP("Text", textArea.transform, "", 13f, FontStyles.Normal, textPrimary);
            StretchFull(inputText.gameObject);
            field.textComponent = inputText;
            field.textViewport = textArea.GetComponent<RectTransform>();

            var ph = MakeTMP("Placeholder", textArea.transform, placeholder, 13f, FontStyles.Italic, textSecondary);
            StretchFull(ph.gameObject);
            field.placeholder = ph;

            return field;
        }

        private (Slider, TMP_Text) MakeSliderRow(Transform parent, string label, int min, int max, int def)
        {
            var row = MakeHRow($"Slider_{label}", parent, 32f, 8f);

            var lbl = MakeTMP("Label", row.transform, label, 12f, FontStyles.Normal, textSecondary);
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 0);
            var lblLE = lbl.gameObject.AddComponent<LayoutElement>();
            lblLE.preferredWidth = 150f;
            lblLE.flexibleWidth = 0f;

            var sliderGO = MakeRect("Slider", row.transform);
            var sliderLE = sliderGO.AddComponent<LayoutElement>();
            sliderLE.flexibleWidth = 1f;

            // Slider background
            var sliderImg = sliderGO.AddComponent<Image>();
            sliderImg.color = inputBg;
            SetRadius(sliderImg, 4f);
            sliderGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 6f);

            var slider = sliderGO.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;
            slider.value = def;

            // Fill area
            var fillArea = MakeRect("Fill Area", sliderGO.transform);
            StretchFull(fillArea);
            var fillAreaRT = fillArea.GetComponent<RectTransform>();
            fillAreaRT.offsetMin = new Vector2(0, 0);
            fillAreaRT.offsetMax = new Vector2(-10, 0);

            var fill = MakeRect("Fill", fillArea.transform);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = accentColor;
            SetRadius(fillImg, 4f);
            slider.fillRect = fill.GetComponent<RectTransform>();

            // Handle
            var handleArea = MakeRect("Handle Slide Area", sliderGO.transform);
            StretchFull(handleArea);

            var handle = MakeRect("Handle", handleArea.transform);
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(18f, 18f);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            SetRadius(handleImg, 9f);
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handleImg;

            // Value label
            var valLabel = MakeTMP("Value", row.transform, def.ToString(), 13f, FontStyles.Bold, textPrimary);
            valLabel.alignment = TextAlignmentOptions.Center;
            valLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 0);
            var valLE = valLabel.gameObject.AddComponent<LayoutElement>();
            valLE.preferredWidth = 30f;
            valLE.flexibleWidth = 0f;

            slider.onValueChanged.AddListener(v => valLabel.text = ((int)v).ToString());

            return (slider, valLabel);
        }

        private void MakeTabButton(Transform parent, string label, bool active, ref Button btnRef)
        {
            var go = MakeRect(label, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 36f);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            var img = go.AddComponent<Image>();
            img.color = active ? accentColor : new Color(0.15f, 0.12f, 0.25f, 1f);
            SetRadius(img, 8f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = buttonHover;
            colors.pressedColor = accentColor * 0.8f;
            btn.colors = colors;

            var txt = MakeTMP("Text", go.transform, label, 13f, FontStyles.Normal, textPrimary);
            txt.alignment = TextAlignmentOptions.Center;
            StretchFull(txt.gameObject);

            btnRef = btn;
        }

        private void SetTabActive(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? accentColor : new Color(0.15f, 0.12f, 0.25f, 1f);
        }

        private Button MakeButton(string name, Transform parent, string label,
                                   Color bg, Color fg, float height)
        {
            var go = MakeRect(name, parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            var img = go.AddComponent<Image>();
            img.color = bg;
            SetRadius(img, 10f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = bg * 1.2f;
            colors.pressedColor = bg * 0.8f;
            btn.colors = colors;

            var txt = MakeTMP("Label", go.transform, label, 15f, FontStyles.Bold, fg);
            txt.alignment = TextAlignmentOptions.Center;
            StretchFull(txt.gameObject);

            return btn;
        }

        private void AddSeparator(Transform parent)
        {
            var sep = MakeRect("Sep", parent);
            sep.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 1f);
            var img = sep.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.08f);
        }

        // ── Primitives ────────────────────────────────────────────────────────

        private GameObject MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private GameObject MakeImage(string name, Transform parent, Color color)
        {
            var go = MakeRect(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private TMP_Text MakeTMP(string name, Transform parent, string text,
                                  float size, FontStyles style, Color color)
        {
            var go = MakeRect(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            return tmp;
        }

        private GameObject MakeHRow(string name, Transform parent, float height, float spacing)
        {
            var go = MakeRect(name, parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            return go;
        }

        private void StretchFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void SetRadius(Image img, float radius)
        {
            // Requiere un sprite con borde redondeado — usamos sliced si disponible
            // Para esquinas redondeadas sin sprite especial usamos este hack
            img.pixelsPerUnitMultiplier = 1f;
        }
    }
}