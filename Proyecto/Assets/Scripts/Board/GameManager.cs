using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// GameManager — orquesta todo el juego.
    ///
    /// FLUJO:
    ///   Start → asigna elementos aleatoriamente → reparte cartas → comienza turno 0
    ///   Turno: lanzar dado → elegir ficha → mover → [combate si hay enemigo] → siguiente turno
    ///
    /// SETUP EN UNITY:
    ///   1. Empty GameObject "GameManager" → adjunta este script
    ///   2. Asigna boardGenerator, piecePrefab, deckManager
    ///   3. Asigna hudController (el script del Canvas HUD)
    ///   4. Dale Play
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Sistemas")]
        public BoardGenerator boardGenerator;
        public DeckManager deckManager;
        public HUDController hudController;
        public CombatManager combatManager;

        [Header("Prefab de ficha")]
        public GameObject piecePrefab;

        [Header("Configuración")]
        public int numberOfPlayers = 4;
        public int piecesPerPlayer = 3;
        public string[] playerNames = { "Jugador 1", "Jugador 2", "Jugador 3", "Jugador 4" };
        [Range(1, 6)] public int diceMin = 1;
        [Range(1, 6)] public int diceMax = 6;

        // ── Runtime ──────────────────────────────────────────────────────────
        public List<PlayerData> Players { get; private set; } = new();
        public int TurnIndex { get; private set; } = 0;
        public PlayerData CurrentPlayer => Players[TurnIndex];

        public enum GameState { Setup, WaitingForRoll, WaitingForPieceSelect, Moving, Combat, GameOver }
        public GameState State { get; private set; } = GameState.Setup;

        private int _diceResult;
        private bool _isProcessing;

        // ── Lifecycle ────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            AutoFindComponents();

            if (boardGenerator == null)
            {
                Debug.LogError("[GameManager] No se encontró BoardGenerator."); return;
            }

            // Solo genera si no se generó en Awake del BoardGenerator
            if (boardGenerator.startTiles == null)
                boardGenerator.GenerateBoard();

            if (boardGenerator.startTiles == null)
            {
                Debug.LogError("[GameManager] startTiles es null tras GenerateBoard."); return;
            }

            AssignRandomElements();
            DealCards();
            SpawnPieces();

            hudController?.Initialize(Players);
            StartCoroutine(BeginTurnCoroutine());
        }

        // ── Inicialización ───────────────────────────────────────────────────

        /// <summary>Asigna un elemento distinto a cada jugador de forma aleatoria.</summary>
        private void AssignRandomElements()
        {
            Players.Clear();

            List<ElementType> pool = new()
                { ElementType.Water, ElementType.Fire, ElementType.Earth, ElementType.Air };

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            for (int p = 0; p < numberOfPlayers; p++)
            {
                PlayerData pd = new()
                {
                    index = p,
                    playerName = p < playerNames.Length ? playerNames[p] : $"Jugador {p + 1}",
                    element = pool[p],
                };
                Players.Add(pd);
                Debug.Log($"[GameManager] {pd.playerName} → {pd.ElementName}");
            }
        }

        private void DealCards()
        {
            if (deckManager == null) deckManager = FindOrAddDeckManager();
            foreach (PlayerData pd in Players)
                pd.hand = deckManager.DealHand(pd.element);
        }

        private void SpawnPieces()
        {
            if (piecePrefab == null) { Debug.LogError("[GameManager] piecePrefab no asignado."); return; }

            foreach (PlayerData pd in Players)
            {
                Tile startTile = boardGenerator.GetStartTileByElement(pd.element);
                if (startTile == null)
                {
                    Debug.LogError($"[GameManager] startTile null para jugador {pd.index} — saltando.");
                    continue;
                }

                for (int i = 0; i < piecesPerPlayer; i++)
                {
                    GameObject go = Instantiate(piecePrefab);
                    go.name = $"Piece_{pd.playerName}_{i}";
                    go.transform.localScale = Vector3.one * 0.5f;

                    if (go.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.material = new Material(rend.sharedMaterial != null
                            ? rend.sharedMaterial : rend.material);
                        rend.material.color = pd.ElementColor;
                    }

                    Piece piece = go.GetComponent<Piece>() ?? go.AddComponent<Piece>();
                    piece.playerIndex = pd.index;
                    piece.pieceIndex = i;
                    piece.element = pd.element;

                    // Posiciona directamente sobre la startTile
                    Vector3 tilePos = startTile.transform.position;
                    go.transform.position = tilePos + Vector3.up * 1.2f
                                          + Vector3.right * (i - 1) * 0.5f;
                    piece.currentTile = startTile;

                    pd.pieces.Add(piece);
                    Debug.Log($"[GameManager] {go.name} → {go.transform.position}");
                }
            }
        }

        // ── Flujo de turno ───────────────────────────────────────────────────
        private IEnumerator BeginTurnCoroutine()
        {
            yield return new WaitForSeconds(0.4f);
            BeginTurn();
        }

        public void BeginTurn()
        {
            State = GameState.WaitingForRoll;
            _isProcessing = false;
            hudController?.ShowTurn(CurrentPlayer);
            Log($"Turno de {CurrentPlayer.playerName} ({CurrentPlayer.ElementName})");
        }

        public void OnRollDice()
        {
            if (State != GameState.WaitingForRoll || _isProcessing) return;
            StartCoroutine(RollDiceCoroutine());
        }

        private IEnumerator RollDiceCoroutine()
        {
            _isProcessing = true;
            State = GameState.WaitingForPieceSelect;

            for (float t = 0; t < 0.7f; t += 0.09f)
            {
                hudController?.ShowDiceRoll(Random.Range(diceMin, diceMax + 1));
                yield return new WaitForSeconds(0.09f);
            }

            _diceResult = Random.Range(diceMin, diceMax + 1);
            hudController?.ShowDiceRoll(_diceResult);
            hudController?.SetMessage($"Dado: {_diceResult} — Elige una ficha");
            Log($"Dado: {_diceResult}");
            _isProcessing = false;
        }

        private Piece _selectedPiece;
        private List<Tile> _availableDirections = new();

        private void Update()
        {
            if (_isProcessing) return;
            if (!Input.GetMouseButtonDown(0)) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

            if (State == GameState.WaitingForPieceSelect)
            {
                // Si ya hay ficha seleccionada, espera click en tile de dirección
                if (_selectedPiece != null)
                {
                    Tile t = hit.collider.GetComponentInParent<Tile>();
                    if (t != null && _availableDirections.Contains(t))
                    { ConfirmDirection(t); return; }
                }

                // Click en una ficha del jugador actual
                Piece p = hit.collider.GetComponentInParent<Piece>();
                if (p != null && p.playerIndex == TurnIndex)
                    SelectPiece(p);
            }
        }

        private void SelectPiece(Piece piece)
        {
            _selectedPiece = piece;
            piece.PlaySelectAnimation();

            // Filtra direcciones — excluye tiles ocupadas por fichas propias
            var allDirs = piece.GetAvailableDirections();
            var directions = new List<Tile>();
            foreach (Tile t in allDirs)
                if (!IsTileOccupiedByAlly(t, piece.playerIndex))
                    directions.Add(t);

            if (directions.Count == 0)
            {
                Log("No hay movimientos disponibles — turno saltado.");
                _selectedPiece = null;
                NextTurn();
                return;
            }

            if (directions.Count == 1)
            {
                ClearHighlights();
                StartCoroutine(MoveAndCheck(piece, _diceResult, directions[0]));
                return;
            }

            // Múltiples direcciones — destaca y espera click
            _availableDirections = directions;
            foreach (Tile t in directions)
                t.SetHighlight(true);

            hudController?.SetMessage("Elige hacia dónde mover");
            Log("Elige dirección (click en casilla amarilla)");
        }

        private bool IsTileOccupiedByAlly(Tile tile, int myPlayerIndex)
        {
            foreach (PlayerData pd in Players)
            {
                if (pd.index != myPlayerIndex) continue;
                foreach (Piece p in pd.pieces)
                    if (p.currentTile == tile) return true;
            }
            return false;
        }

        private void ConfirmDirection(Tile chosenTile)
        {
            ClearHighlights();
            StartCoroutine(MoveAndCheck(_selectedPiece, _diceResult, chosenTile));
            _selectedPiece = null;
        }

        private void ClearHighlights()
        {
            foreach (Tile t in _availableDirections)
                if (t != null) t.SetHighlight(false);
            _availableDirections.Clear();
        }

        public void OnPieceSelected(Piece piece)
        {
            if (State != GameState.WaitingForPieceSelect || _isProcessing) return;
            if (piece.playerIndex != TurnIndex) return;
            SelectPiece(piece);
        }

        private IEnumerator MoveAndCheck(Piece piece, int steps, Tile firstStep = null)
        {
            _isProcessing = true;
            State = GameState.Moving;

            Piece enemyFound = null;
            Tile combatTile = null;

            piece.OnArrivedAtTile = tile =>
            {
                if (enemyFound != null) return;
                enemyFound = GetEnemyOn(tile, piece.playerIndex);
                if (enemyFound != null) combatTile = tile;
            };

            if (firstStep != null)
                piece.MoveStepsWithDirection(steps, firstStep);
            else
                piece.MoveSteps(steps);

            yield return new WaitUntil(() => !piece.isMoving);
            piece.OnArrivedAtTile = null;
            CurrentPlayer.RestoreEnergy();

            if (enemyFound != null)
            {
                Log($"¡Encuentro! {CurrentPlayer.playerName} vs {Players[enemyFound.playerIndex].playerName}");
                yield return StartCoroutine(StartCombat(piece, enemyFound));
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
                NextTurn();
            }
            _isProcessing = false;
        }

        // ── Combate ──────────────────────────────────────────────────────────
        private IEnumerator StartCombat(Piece attacker, Piece defender)
        {
            State = GameState.Combat;
            PlayerData atkPD = Players[attacker.playerIndex];
            PlayerData defPD = Players[defender.playerIndex];
            hudController?.ShowCombatPanel(atkPD, defPD);

            if (combatManager != null)
            {
                bool done = false;
                combatManager.StartCombat(attacker, defender, (winner, loser) =>
                {
                    HandleCombatResult(winner, loser);
                    done = true;
                });
                yield return new WaitUntil(() => done);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
                int atkRoll = Random.Range(1, 7);
                int defRoll = Random.Range(1, 7);
                Piece winner = atkRoll >= defRoll ? attacker : defender;
                Piece loser = atkRoll >= defRoll ? defender : attacker;
                HandleCombatResult(winner, loser);
            }

            hudController?.HideCombatPanel();
            NextTurn();
        }

        private void HandleCombatResult(Piece winner, Piece loser)
        {
            PlayerData winPD = Players[winner.playerIndex];
            PlayerData losePD = Players[loser.playerIndex];
            int dmg = Random.Range(3, 8);
            losePD.TakeDamage(dmg);
            winPD.score += 10;
            Log($"{winPD.playerName} gana! {losePD.playerName} pierde {dmg} HP.");
            hudController?.UpdatePlayerStats(winPD);
            hudController?.UpdatePlayerStats(losePD);
            loser.PlaceOnTile(boardGenerator.GetStartTileByElement((ElementType)loser.playerIndex));
        }

        private void NextTurn()
        {
            TurnIndex = (TurnIndex + 1) % numberOfPlayers;
            BeginTurn();
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private Piece GetEnemyOn(Tile tile, int myPlayer)
        {
            foreach (PlayerData pd in Players)
            {
                if (pd.index == myPlayer) continue;
                foreach (Piece p in pd.pieces)
                    if (p.currentTile == tile) return p;
            }
            return null;
        }

        private void AutoFindComponents()
        {
            if (boardGenerator == null) boardGenerator = Object.FindFirstObjectByType<BoardGenerator>();
            if (deckManager == null) deckManager = Object.FindFirstObjectByType<DeckManager>();
            if (hudController == null) hudController = Object.FindFirstObjectByType<HUDController>();
            if (combatManager == null) combatManager = Object.FindFirstObjectByType<CombatManager>();
        }

        private DeckManager FindOrAddDeckManager()
        {
            var dm = Object.FindFirstObjectByType<DeckManager>();
            return dm != null ? dm : gameObject.AddComponent<DeckManager>();
        }

        public void Log(string msg)
        {
            Debug.Log($"[Game] {msg}");
            hudController?.AddToLog(msg);
        }

        private void OnGUI()
        {
            if (hudController != null) return;
            GUI.Box(new Rect(10, 10, 300, 80), "");
            GUI.Label(new Rect(16, 14, 290, 70),
                $"Turno: {CurrentPlayer?.playerName} ({CurrentPlayer?.ElementName})\nEstado: {State}  Dado: {_diceResult}");
            if (State == GameState.WaitingForRoll)
                if (GUI.Button(new Rect(Screen.width / 2 - 70, Screen.height - 70, 140, 44), "Lanzar dado"))
                    OnRollDice();
        }
    }
}