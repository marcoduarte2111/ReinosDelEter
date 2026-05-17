using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// Singleton que persiste entre escenas y transporta la configuración
    /// desde el menú principal hasta la escena de juego.
    /// </summary>
    public class GameConfig : MonoBehaviour
    {
        public static GameConfig Instance { get; private set; }

        [Header("Jugadores")]
        public string[] playerNames = { "Jugador 1", "Jugador 2", "Jugador 3", "Jugador 4" };

        [Header("Tablero")]
        public int tilesPerArm = 5;
        public int ringTilesPerSide = 4;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}