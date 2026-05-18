using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace ReinosDelEter
{
    public class MainMenuBuilder : MonoBehaviour
    {
        [Header("Imagen de fondo (opcional)")]
        public Sprite backgroundSprite;

        [Header("Escena de juego")]
        public string gameSceneName = "Game";

        private Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
        private Color _accent => C(0.33f, 0.29f, 0.72f);
        private Color _cardBg => C(0.07f, 0.05f, 0.13f, 0.97f);
        private Color _inputBg => C(0.13f, 0.09f, 0.20f);
        private Color _textPri => C(0.95f, 0.93f, 1.00f);
        private Color _textSec => C(0.58f, 0.55f, 0.73f);
        private Color _sep => C(1f, 1f, 1f, 0.07f);
        private Color[] _dotCol => new Color[]{
            C(0.25f,0.55f,1.0f),C(1.0f,0.32f,0.15f),
            C(0.20f,0.78f,0.20f),C(0.75f,0.75f,0.88f)};

        private TMP_InputField[] _inputs = new TMP_InputField[4];
        private Slider _armS, _ringS;
        private TMP_Text _armV, _ringV;
        private GameObject _coverScreen, _configScreen, _quickPanel, _customPanel;
        private Button _quickBtn, _customBtn;
        private bool _isCustom;
        private Canvas _canvas;

        // Font sizes
        private const float F_TITLE = 86f;
        private const float F_SUB = 26f;
        private const float F_HEADING = 32f;
        private const float F_LABEL = 20f;
        private const float F_BODY = 18f;
        private const float F_SMALL = 16f;
        private const float F_BTN = 22f;
        private const float F_BTNBIG = 26f;

        private void Start() { BuildCanvas(); BuildCoverScreen(); BuildConfigScreen(); ShowCover(); }

        private void BuildCanvas()
        {
            var go = new GameObject("Canvas");
            _canvas = go.AddComponent<Canvas>(); _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 10;
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
        }
        private Transform CT => _canvas.transform;

        private void BuildCoverScreen()
        {
            _coverScreen = FullScreen("Cover"); var T = _coverScreen.transform;
            var bg = RT("BG", T, 0, 0); Stretch(bg);
            var bgi = bg.gameObject.AddComponent<Image>();
            bgi.color = backgroundSprite != null ? Color.white : C(0.05f, 0.03f, 0.10f);
            if (backgroundSprite != null) { bgi.sprite = backgroundSprite; bgi.type = Image.Type.Simple; }
            var ov = RT("Ov", T, 0, 0); Stretch(ov); ov.gameObject.AddComponent<Image>().color = C(0.03f, 0.02f, 0.08f, 0.62f);

            var title = TMP("Title", T, "Reinos del Éter", F_TITLE, FontStyles.Bold, _textPri, 1100, 110, 0, 140);
            title.alignment = TextAlignmentOptions.Center;
            var sub = TMP("Sub", T, "Un juego de conquista elemental", F_SUB, FontStyles.Italic, _textSec, 700, 40, 0, 60);
            sub.alignment = TextAlignmentOptions.Center;
            Line("L", T, 0, 10);

            var s = Btn("BtnPlay", T, "Jugar", _accent, _textPri, 380, 72, 0, -80); s.onClick.AddListener(ShowConfig);
            var q = Btn("BtnQuit", T, "Salir del juego", C(0.14f, 0.10f, 0.22f), _textSec, 380, 54, 0, -170); q.onClick.AddListener(QuitGame);
        }

        private void BuildConfigScreen()
        {
            _configScreen = FullScreen("Config"); var T = _configScreen.transform;
            RT("BG2", T, 0, 0).Let(r => { Stretch(r); r.gameObject.AddComponent<Image>().color = C(0.03f, 0.02f, 0.08f, 0.96f); });

            var card = RT("Card", T, 640, 780); AnchorCenter(card); card.anchoredPosition = Vector2.zero;
            card.gameObject.AddComponent<Image>().color = _cardBg;

            float y = 340f;
            TMP("T2", card.transform, "Configurar partida", F_HEADING, FontStyles.Bold, _textPri, 580, 46, 0, y); y -= 60;
            Sep(card.transform, y); y -= 26;

            var tabRow = RT("Tabs", card.transform, 580, 50); AnchorCenter(tabRow); tabRow.anchoredPosition = new Vector2(0, y); y -= 68;
            MakeTab(tabRow.transform, "Inicio rápido", -146, true, out _quickBtn);
            MakeTab(tabRow.transform, "Personalizar", 146, false, out _customBtn);

            var qpRT = RT("QP", card.transform, 580, 70); AnchorCenter(qpRT); qpRT.anchoredPosition = new Vector2(0, y);
            TxtCenter("QT", qpRT.transform, "Los jugadores se llamarán Jugador 1–4.\nLos elementos se asignan al azar.", F_BODY, _textSec, 550, 64);
            _quickPanel = qpRT.gameObject;

            var cpRT = RT("CP", card.transform, 580, 220); AnchorCenter(cpRT); cpRT.anchoredPosition = new Vector2(0, y - 14);
            float iy = 80f;
            for (int i = 0; i < 4; i++) { _inputs[i] = InputRow(cpRT.transform, $"Jugador {i + 1}", _dotCol[i], iy); iy -= 50; }
            _customPanel = cpRT.gameObject; _customPanel.SetActive(false);

            y -= 220;
            Sep(card.transform, y); y -= 26;
            TxtCenter("BLbl", card.transform, "Tablero", F_SMALL, _textSec, 560, 22, 0, y); y -= 32;
            SliderRow(card.transform, "Tiles por brazo diagonal", 3, 10, 5, y, out _armS, out _armV); y -= 44;
            SliderRow(card.transform, "Tiles por lado del anillo", 2, 8, 4, y, out _ringS, out _ringV); y -= 54;
            Sep(card.transform, y); y -= 26;

            var bs = Btn("BSt", card.transform, "Iniciar partida", _accent, _textPri, 540, 60, 0, y); y -= 70; bs.onClick.AddListener(StartGame);
            var bb = Btn("BBk", card.transform, "Volver", C(0.14f, 0.10f, 0.22f), _textSec, 540, 44, 0, y); bb.onClick.AddListener(ShowCover);

            _quickBtn.onClick.AddListener(() => { _isCustom = false; _quickPanel.SetActive(true); _customPanel.SetActive(false); TabCol(_quickBtn, true); TabCol(_customBtn, false); });
            _customBtn.onClick.AddListener(() => { _isCustom = true; _quickPanel.SetActive(false); _customPanel.SetActive(true); TabCol(_quickBtn, false); TabCol(_customBtn, true); });
        }

        private void ShowCover() { _coverScreen.SetActive(true); _configScreen.SetActive(false); }
        private void ShowConfig() { _coverScreen.SetActive(false); _configScreen.SetActive(true); }

        private void StartGame()
        {
            if (GameConfig.Instance == null) new GameObject("GameConfig").AddComponent<GameConfig>();
            var n = new string[4];
            for (int i = 0; i < 4; i++) { string s = _isCustom && _inputs[i] != null ? _inputs[i].text.Trim() : ""; n[i] = s.Length > 0 ? s : $"Jugador {i + 1}"; }
            GameConfig.Instance.playerNames = n;
            GameConfig.Instance.tilesPerArm = _armS != null ? (int)_armS.value : 5;
            GameConfig.Instance.ringTilesPerSide = _ringS != null ? (int)_ringS.value : 4;
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

        private void MakeTab(Transform parent, string label, float x, bool active, out Button btn)
        {
            var r = RT("Tab_" + label, parent, 270, 46); AnchorCenter(r); r.anchoredPosition = new Vector2(x, 0);
            var img = r.gameObject.AddComponent<Image>(); img.color = active ? _accent : _inputBg;
            btn = r.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
            TxtCenter("T", r.transform, label, F_LABEL, _textPri, 260, 42);
        }
        private void TabCol(Button b, bool on) { if (b) b.GetComponent<Image>().color = on ? _accent : _inputBg; }

        private TMP_InputField InputRow(Transform parent, string ph, Color dot, float y)
        {
            var row = RT("Row_" + ph, parent, 540, 44); AnchorCenter(row); row.anchoredPosition = new Vector2(0, y);
            var d = RT("D", row, 12, 12); d.anchorMin = new Vector2(0, 0.5f); d.anchorMax = new Vector2(0, 0.5f); d.pivot = new Vector2(0, 0.5f); d.anchoredPosition = new Vector2(4, 0);
            d.gameObject.AddComponent<Image>().color = dot;
            var bg = RT("IB", row, 510, 40); bg.anchorMin = new Vector2(0.5f, 0.5f); bg.anchorMax = new Vector2(0.5f, 0.5f); bg.pivot = new Vector2(0.5f, 0.5f); bg.anchoredPosition = new Vector2(12, 0);
            bg.gameObject.AddComponent<Image>().color = _inputBg;
            var f = bg.gameObject.AddComponent<TMP_InputField>();
            var ta = RT("TA", bg, 490, 36); AnchorCenter(ta); ta.anchoredPosition = Vector2.zero;
            ta.gameObject.AddComponent<RectMask2D>();
            var it = ta.gameObject.AddComponent<TextMeshProUGUI>(); it.fontSize = F_BODY; it.color = _textPri;
            f.textComponent = it; f.textViewport = ta;
            var pg = RT("PH", bg, 490, 36); AnchorCenter(pg); pg.anchoredPosition = Vector2.zero;
            var pt = pg.gameObject.AddComponent<TextMeshProUGUI>(); pt.text = ph; pt.fontSize = F_BODY; pt.fontStyle = FontStyles.Italic; pt.color = C(_textSec.r, _textSec.g, _textSec.b, 0.55f);
            f.placeholder = pt; return f;
        }

        private void SliderRow(Transform parent, string label, int min, int max, int def, float y, out Slider s, out TMP_Text val)
        {
            var row = RT("SR_" + label, parent, 560, 30); AnchorCenter(row); row.anchoredPosition = new Vector2(0, y);
            var lGO = RT("L", row, 240, 26); lGO.anchorMin = new Vector2(0, 0.5f); lGO.anchorMax = new Vector2(0, 0.5f); lGO.pivot = new Vector2(0, 0.5f); lGO.anchoredPosition = Vector2.zero;
            var lT = lGO.gameObject.AddComponent<TextMeshProUGUI>(); lT.text = label; lT.fontSize = F_SMALL; lT.color = _textSec; lT.alignment = TextAlignmentOptions.Left;
            var sGO = RT("S", row, 260, 10); sGO.anchorMin = new Vector2(0, 0.5f); sGO.anchorMax = new Vector2(0, 0.5f); sGO.pivot = new Vector2(0, 0.5f); sGO.anchoredPosition = new Vector2(248, 0);
            sGO.gameObject.AddComponent<Image>().color = _inputBg;
            s = sGO.gameObject.AddComponent<Slider>(); s.minValue = min; s.maxValue = max; s.wholeNumbers = true; s.value = def;
            var fa = RT("FA", sGO, 252, 10); fa.anchorMin = Vector2.zero; fa.anchorMax = new Vector2(1, 1); fa.offsetMin = Vector2.zero; fa.offsetMax = new Vector2(-8, 0);
            var fi = RT("F", fa, 0, 10); fi.anchorMin = Vector2.zero; fi.anchorMax = new Vector2(0, 1);
            fi.gameObject.AddComponent<Image>().color = _accent; s.fillRect = fi;
            var ha = RT("HA", sGO, 260, 10); ha.anchorMin = Vector2.zero; ha.anchorMax = Vector2.one; ha.offsetMin = ha.offsetMax = Vector2.zero;
            var h = RT("H", ha, 20, 20); h.anchorMin = h.anchorMax = new Vector2(0, 0.5f);
            var hi = h.gameObject.AddComponent<Image>(); hi.color = Color.white; s.handleRect = h; s.targetGraphic = hi;
            var vGO = RT("V", row, 40, 26); vGO.anchorMin = new Vector2(0, 0.5f); vGO.anchorMax = new Vector2(0, 0.5f); vGO.pivot = new Vector2(0, 0.5f); vGO.anchoredPosition = new Vector2(518, 0);
            var localVal = vGO.gameObject.AddComponent<TextMeshProUGUI>(); localVal.text = def.ToString(); localVal.fontSize = F_LABEL; localVal.fontStyle = FontStyles.Bold; localVal.color = _textPri; localVal.alignment = TextAlignmentOptions.Center;
            s.onValueChanged.AddListener(v => localVal.text = ((int)v).ToString());
            val = localVal;
        }

        // ── Primitives ────────────────────────────────────────────────────────
        private GameObject FullScreen(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(CT, false);
            var rt = go.AddComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }
        private RectTransform RT(string name, Transform parent, float w, float h)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>(); rt.sizeDelta = new Vector2(w, h); return rt;
        }
        private void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        private void AnchorCenter(RectTransform rt) { rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); }
        private Button Btn(string name, Transform parent, string label, Color bg, Color fg, float w, float h, float x, float y)
        {
            var r = RT(name, parent, w, h); AnchorCenter(r); r.anchoredPosition = new Vector2(x, y);
            var img = r.gameObject.AddComponent<Image>(); img.color = bg;
            var btn = r.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
            var cs = btn.colors; cs.highlightedColor = Color.Lerp(bg, Color.white, 0.15f); cs.pressedColor = Color.Lerp(bg, Color.black, 0.15f); btn.colors = cs;
            TxtCenter("L", r.transform, label, w > 350 ? F_BTNBIG : F_BTN, fg, (int)(w - 20), (int)(h - 8));
            return btn;
        }
        private TMP_Text TMP(string name, Transform parent, string text, float size, FontStyles style, Color col, float w, float h, float x, float y)
        {
            var r = RT(name, parent, w, h); AnchorCenter(r); r.anchoredPosition = new Vector2(x, y);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.text = text; t.fontSize = size; t.fontStyle = style; t.color = col; t.alignment = TextAlignmentOptions.Center;
            return t;
        }
        private void TxtCenter(string name, Transform parent, string text, float size, Color col, float w, float h)
        {
            var r = RT(name, parent, w, h); AnchorCenter(r); r.anchoredPosition = Vector2.zero;
            var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.text = text; t.fontSize = size; t.color = col; t.alignment = TextAlignmentOptions.Center;
        }
        private void TxtCenter(string name, Transform parent, string text, float size, Color col, float w, float h, float x, float y)
        {
            var r = RT(name, parent, w, h); AnchorCenter(r); r.anchoredPosition = new Vector2(x, y);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.text = text; t.fontSize = size; t.color = col; t.alignment = TextAlignmentOptions.Center;
        }
        private void Sep(Transform parent, float y)
        {
            var r = RT("Sep", parent, 560, 1); AnchorCenter(r); r.anchoredPosition = new Vector2(0, y);
            r.gameObject.AddComponent<Image>().color = _sep;
        }
        private void Line(string name, Transform parent, float x, float y)
        {
            var r = RT(name, parent, 220, 2); AnchorCenter(r); r.anchoredPosition = new Vector2(x, y);
            r.gameObject.AddComponent<Image>().color = C(_accent.r, _accent.g, _accent.b, 0.5f);
        }
    }
    internal static class RTExt
    {
        internal static RectTransform Let(this RectTransform rt, System.Action<RectTransform> a) { a(rt); return rt; }
    }
}