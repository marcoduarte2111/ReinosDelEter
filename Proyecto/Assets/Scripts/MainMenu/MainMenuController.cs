using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace ReinosDelEter
{
    /// <summary>
    /// MainMenuController — maneja la pantalla de inicio.
    ///
    /// SETUP EN UNITY:
    ///   1. Crea una nueva escena llamada "MainMenu"
    ///   2. Crea un Canvas con estos elementos:
    ///
    ///      [Canvas]
    ///        ├── TitleLabel (TMP)
    ///        ├── QuickStartButton (Button)
    ///        ├── CustomizeButton (Button)
    ///        ├── QuickStartPanel
    ///        │     └── (solo texto informativo)
    ///        ├── CustomPanel
    ///        │     ├── NameInput_0 (TMP_InputField)
    ///        │     ├── NameInput_1
    ///        │     ├── NameInput_2
    ///        │     └── NameInput_3
    ///        ├── ArmSlider (Slider) + ArmValueLabel (TMP)
    ///        ├── RingSlider (Slider) + RingValueLabel (TMP)
    ///        ├── StartButton (Button)
    ///        └── QuitButton (Button)
    ///
    ///   3. Asigna las referencias en el Inspector
    ///   4. En Build Settings agrega ambas escenas:
    ///      Index 0: MainMenu
    ///      Index 1: Game (tu escena actual)
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Tabs")]
        public Button quickStartButton;
        public Button customizeButton;
        public GameObject quickStartPanel;
        public GameObject customPanel;

        [Header("Nombres de jugadores")]
        public TMP_InputField[] nameInputs = new TMP_InputField[4];

        [Header("Sliders de tablero")]
        public Slider armSlider;
        public TMP_Text armValueLabel;
        public Slider ringSlider;
        public TMP_Text ringValueLabel;

        [Header("Botones principales")]
        public Button startButton;
        public Button quitButton;

        [Header("Nombre de la escena de juego")]
        public string gameSceneName = "Game";

        private bool _isCustomMode = false;

        private void Start()
        {
            // Asegura que exista el GameConfig
            if (GameConfig.Instance == null)
            {
                var go = new GameObject("GameConfig");
                go.AddComponent<GameConfig>();
            }

            // Valores iniciales de sliders
            if (armSlider != null)
            {
                armSlider.minValue = 3;
                armSlider.maxValue = 10;
                armSlider.wholeNumbers = true;
                armSlider.value = GameConfig.Instance.tilesPerArm;
                armSlider.onValueChanged.AddListener(v =>
                {
                    if (armValueLabel != null) armValueLabel.text = ((int)v).ToString();
                });
                if (armValueLabel != null) armValueLabel.text = ((int)armSlider.value).ToString();
            }

            if (ringSlider != null)
            {
                ringSlider.minValue = 2;
                ringSlider.maxValue = 8;
                ringSlider.wholeNumbers = true;
                ringSlider.value = GameConfig.Instance.ringTilesPerSide;
                ringSlider.onValueChanged.AddListener(v =>
                {
                    if (ringValueLabel != null) ringValueLabel.text = ((int)v).ToString();
                });
                if (ringValueLabel != null) ringValueLabel.text = ((int)ringSlider.value).ToString();
            }

            // Botones
            quickStartButton?.onClick.AddListener(ShowQuickStart);
            customizeButton?.onClick.AddListener(ShowCustomize);
            startButton?.onClick.AddListener(StartGame);
            quitButton?.onClick.AddListener(QuitGame);

            // Estado inicial
            ShowQuickStart();
        }

        public void ShowQuickStart()
        {
            _isCustomMode = false;
            if (quickStartPanel != null) quickStartPanel.SetActive(true);
            if (customPanel != null) customPanel.SetActive(false);
        }

        public void ShowCustomize()
        {
            _isCustomMode = true;
            if (quickStartPanel != null) quickStartPanel.SetActive(false);
            if (customPanel != null) customPanel.SetActive(true);
        }

        public void StartGame()
        {
            // Lee nombres
            string[] names = new string[4];
            for (int i = 0; i < 4; i++)
            {
                if (_isCustomMode && nameInputs != null && i < nameInputs.Length
                    && nameInputs[i] != null && nameInputs[i].text.Trim().Length > 0)
                    names[i] = nameInputs[i].text.Trim();
                else
                    names[i] = $"Jugador {i + 1}";
            }

            // Guarda en GameConfig
            GameConfig.Instance.playerNames = names;
            GameConfig.Instance.tilesPerArm = armSlider != null ? (int)armSlider.value : 5;
            GameConfig.Instance.ringTilesPerSide = ringSlider != null ? (int)ringSlider.value : 4;

            SceneManager.LoadScene(gameSceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}