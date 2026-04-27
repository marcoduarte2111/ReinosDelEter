using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// GameManager — flujo de juego completo.
    ///
    /// TURNO:
    ///   1. Lanzar dado → fichas del jugador brillan
    ///   2. Click en ficha → vecinos se resaltan en amarillo
    ///   3. Click en vecino amarillo → ficha avanza 1 paso
    ///   4. Si llega a cruce (>1 vecino) con pasos restantes → vuelve al paso 2
    ///   5. Si hay enemigo en la tile → combate (puede ser grupal)
    ///   6. Si gana → continúa pasos restantes. Si pierde → turno termina.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

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

        public enum GameState
        {
            Setup, WaitingForRoll, WaitingForPieceSelect,
            WaitingForDirection, Moving, Combat, GameOver
        }
        public GameState State { get; private set; } = GameState.Setup;

        private int _diceResult;
        private bool _isProcessing;
        private Piece _activePiece;
        private int _stepsRemaining;
        private List<Tile> _highlightedTiles = new();

        // ── Lifecycle ────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            AutoFindComponents();
            if (boardGenerator == null) { Debug.LogError("[GM] No BoardGenerator."); return; }
            if (boardGenerator.startTiles == null) boardGenerator.GenerateBoard();
            if (boardGenerator.startTiles == null) { Debug.LogError("[GM] startTiles null."); return; }

            AssignRandomElements();
            DealCards();
            SpawnPieces();
            hudController?.Initialize(Players);
            StartCoroutine(BeginTurnCoroutine());
        }

        private void Update()
        {
            if (_isProcessing) return;
            if (!Input.GetMouseButtonDown(0)) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return;

            switch (State)
            {
                case GameState.WaitingForPieceSelect:
                    HandlePieceClick(hit);
                    break;
                case GameState.WaitingForDirection:
                    HandleDirectionClick(hit);
                    break;
            }
        }

        // ── Click handlers ───────────────────────────────────────────────────

        private void HandlePieceClick(RaycastHit hit)
        {
            Piece p = hit.collider.GetComponent<Piece>()
                   ?? hit.collider.GetComponentInParent<Piece>();
            if (p == null || p.playerIndex != TurnIndex) return;
            SelectPiece(p);
        }

        private void HandleDirectionClick(RaycastHit hit)
        {
            // Click en tile resaltada
            Tile t = hit.collider.GetComponent<Tile>()
                  ?? hit.collider.GetComponentInParent<Tile>();
            if (t != null && _highlightedTiles.Contains(t))
            {
                ConfirmMove(t);
                return;
            }

            // Click en otra ficha propia → cambia selección
            Piece p = hit.collider.GetComponent<Piece>()
                   ?? hit.collider.GetComponentInParent<Piece>();
            if (p != null && p.playerIndex == TurnIndex)
            {
                ClearHighlights();
                SelectPiece(p);
            }
        }

        // ── Selección ────────────────────────────────────────────────────────

        private void SelectPiece(Piece piece)
        {
            ClearPieceGlows();
            piece.SetSelected(true);
            piece.PlaySelectAnimation();
            _activePiece = piece;
            ShowNeighborOptions(piece);
        }

        private void ShowNeighborOptions(Piece piece)
        {
            Tile cameFrom = piece._prevTileAtJunction;
            var options = new List<Tile>();

            foreach (Tile nb in piece.currentTile.neighbors)
            {
                if (nb == null) continue;

                // Excluye de donde vino
                if (nb == cameFrom) continue;

                // Desde el centro: excluye tiles que lleven de vuelta al brazo propio
                // (el centro conecta a las 4 esquinas; excluimos la esquina del reino propio)
                if (piece.currentTile.tileType == TileType.Center
                    && nb.pathIndex == piece.playerIndex)
                    continue;

                // Nunca mostrar el castillo propio como opción de movimiento automático
                if (nb.tileType == TileType.Start && nb.pathIndex == piece.playerIndex)
                    continue;

                options.Add(nb);
            }

            if (options.Count == 0)
            {
                Log("Sin movimientos — turno saltado.");
                _activePiece = null;
                ClearPieceGlows();
                NextTurn();
                return;
            }

            HighlightTiles(options);
            State = GameState.WaitingForDirection;
            hudController?.SetMessage($"Dado: {_stepsRemaining} — Elige dirección");
        }

        private void ConfirmMove(Tile chosen)
        {
            ClearHighlights();
            if (_activePiece == null) return;

            State = GameState.Moving;
            _isProcessing = true;

            _activePiece.stopMovement = false;
            _activePiece.OnArrivedAtTile = OnArrived;
            _activePiece.OnMovementFinished = OnFinished;
            _activePiece.OnReachedJunction = OnJunction;

            _activePiece.ContinueMovement(chosen, _stepsRemaining);
        }

        // ── Movement callbacks ───────────────────────────────────────────────

        private void OnArrived(Tile tile)
        {
            var enemies = GetEnemiesOn(tile, _activePiece.playerIndex);
            Debug.Log($"[OnArrived] tile={tile?.name} activePiece={_activePiece?.name} enemies={enemies.Count}");
            foreach (var e in enemies) Debug.Log($"  enemigo: {e.name} en {e.currentTile?.name}");

            if (enemies.Count > 0)
            {
                _activePiece.stopMovement = true;
                _activePiece.OnArrivedAtTile = null;
                _activePiece.OnMovementFinished = null;
                _activePiece.OnReachedJunction = null;
                StartCoroutine(HandleCombat(_activePiece, enemies));
            }
        }

        private void OnFinished(Tile tile)
        {
            _activePiece.OnArrivedAtTile = null;
            _activePiece.OnMovementFinished = null;
            _activePiece.OnReachedJunction = null;
            StartCoroutine(AfterMove(_activePiece));
        }

        private void OnJunction(Tile tile, int stepsLeft)
        {
            _stepsRemaining = stepsLeft;

            _activePiece.OnArrivedAtTile = null;
            _activePiece.OnMovementFinished = null;
            _activePiece.OnReachedJunction = null;

            _isProcessing = false;
            State = GameState.WaitingForDirection;
            ShowNeighborOptions(_activePiece);
        }

        // ── Combat ───────────────────────────────────────────────────────────

        private IEnumerator HandleCombat(Piece attacker, List<Piece> enemies)
        {
            yield return new WaitUntil(() => !attacker.isMoving);

            // Reúne aliados en la misma tile
            var allies = GetAlliesOn(attacker.currentTile, attacker.playerIndex);
            allies.Insert(0, attacker); // attacker es el primero

            // Por cada ficha enemiga única en la tile, lanza combate grupal
            var defeatedEnemies = new HashSet<Piece>();
            bool attackerLost = false;
            int stepsAfter = Mathf.Max(0, _stepsRemaining - 1);

            foreach (Piece enemy in enemies)
            {
                if (defeatedEnemies.Contains(enemy)) continue;
                if (attackerLost) break;

                Log($"[Combate] {Players[attacker.playerIndex].playerName} " +
                    $"({allies.Count} fichas) vs {Players[enemy.playerIndex].playerName}");

                bool attackerWon = false;
                yield return StartCoroutine(RunGroupCombat(allies, enemy,
                    won => attackerWon = won));

                if (attackerWon)
                {
                    defeatedEnemies.Add(enemy);
                    // Ficha enemiga vuelve a su castillo
                    Tile home = boardGenerator.GetStartTileByElement(enemy.element);
                    if (home != null) enemy.PlaceOnTile(home);
                    Log($"{Players[enemy.playerIndex].playerName} vuelve a su castillo.");
                }
                else
                {
                    attackerLost = true;
                    // Ficha atacante vuelve a su castillo
                    Tile home = boardGenerator.GetStartTileByElement(attacker.element);
                    if (home != null) attacker.PlaceOnTile(home);
                    Log($"{Players[attacker.playerIndex].playerName} pierde y vuelve.");
                }
            }

            if (!attackerLost && stepsAfter > 0)
            {
                // Ganó — continúa con pasos restantes
                Log($"Continúa con {stepsAfter} paso(s).");
                yield return new WaitForSeconds(0.3f);
                _stepsRemaining = stepsAfter;
                _isProcessing = false;
                State = GameState.WaitingForDirection;
                ShowNeighborOptions(attacker);
            }
            else
            {
                _isProcessing = false;
                _activePiece = null;
                NextTurn();
            }
        }

        private IEnumerator RunGroupCombat(List<Piece> attackers, Piece defender,
                                            System.Action<bool> onResult)
        {
            State = GameState.Combat;

            PlayerData atkPD = Players[attackers[0].playerIndex];
            PlayerData defPD = Players[defender.playerIndex];
            hudController?.ShowCombatPanel(atkPD, defPD);

            bool attackerWon = false;

            if (combatManager != null)
            {
                bool done = false;
                combatManager.StartGroupCombat(attackers, defender, won =>
                {
                    attackerWon = won;
                    done = true;
                });
                yield return new WaitUntil(() => done);
            }
            else
            {
                // Fallback: suma ATK de todos los aliados vs 1 dado del defensor
                yield return new WaitForSeconds(1.0f);
                int atkTotal = 0;
                foreach (var a in attackers) atkTotal += Random.Range(1, 7);
                int defRoll = Random.Range(1, 7) * attackers.Count; // defensor escala

                attackerWon = atkTotal >= defRoll;

                int dmg = Random.Range(3, 8);
                if (attackerWon)
                {
                    defPD.TakeDamage(dmg);
                    atkPD.score += 10;
                    Log($"{atkPD.playerName} gana! {defPD.playerName} -{dmg} HP.");
                }
                else
                {
                    atkPD.TakeDamage(dmg);
                    defPD.score += 10;
                    Log($"{defPD.playerName} defiende! {atkPD.playerName} -{dmg} HP.");
                }
                hudController?.UpdatePlayerStats(atkPD);
                hudController?.UpdatePlayerStats(defPD);
            }

            hudController?.HideCombatPanel();
            onResult?.Invoke(attackerWon);
        }

        // ── Post-movimiento ──────────────────────────────────────────────────

        private IEnumerator AfterMove(Piece piece)
        {
            Tile tile = piece.currentTile;

            if (tile != null && tile.IsCastle)
            {
                if (tile.ownedByPlayer == piece.playerIndex)
                    Log($"{CurrentPlayer.playerName} en su castillo.");
                else if (tile.ownedByPlayer == -1)
                    ConquerCastle(piece, tile);
                else
                {
                    tile.Free();
                    ConquerCastle(piece, tile);
                    Log($"{CurrentPlayer.playerName} arrebató un castillo!");
                }
            }

            CurrentPlayer.RestoreEnergy();
            yield return new WaitForSeconds(0.3f);
            _isProcessing = false;
            _activePiece = null;
            NextTurn();
        }

        private void ConquerCastle(Piece piece, Tile castle)
        {
            PlayerData pd = Players[piece.playerIndex];
            int prevOwner = castle.ownedByPlayer;

            castle.Conquer(piece.playerIndex, pd.ElementColor);
            pd.score += 25;
            Log($"[Castillo] {pd.playerName} conquisto un castillo! +25 pts");
            hudController?.UpdatePlayerStats(pd);

            // Chequea si el dueño anterior fue eliminado (todos sus castillos tomados)
            if (prevOwner >= 0 && prevOwner != piece.playerIndex)
                CheckElimination(prevOwner);
        }

        private void CheckElimination(int playerIndex)
        {
            // Cuenta castillos que aún pertenecen a este jugador
            int ownedCastles = 0;
            foreach (var pd in Players)
                if (pd.index != playerIndex)
                    foreach (var p in pd.pieces)
                        if (p.currentTile != null
                            && p.currentTile.IsCastle
                            && p.currentTile.ownedByPlayer == playerIndex)
                            ownedCastles++;

            // También cuenta el castillo natal si aún no fue conquistado
            Tile home = boardGenerator.GetStartTileByElement(Players[playerIndex].element);
            if (home != null && home.ownedByPlayer == playerIndex)
                ownedCastles++;
            if (home != null && home.ownedByPlayer == -1) // nunca fue conquistado = sigue siendo "suyo"
                ownedCastles++;

            if (ownedCastles == 0)
            {
                Log($"[Eliminado] {Players[playerIndex].playerName} fue eliminado!");
                // Devuelve todas sus fichas al castillo natal (que ya fue conquistado)
                foreach (Piece p in Players[playerIndex].pieces)
                    p.gameObject.SetActive(false);
            }
        }

        // ── Turno ────────────────────────────────────────────────────────────

        private IEnumerator BeginTurnCoroutine()
        {
            yield return new WaitForSeconds(0.5f);
            BeginTurn();
        }

        public void BeginTurn()
        {
            State = GameState.WaitingForRoll;
            _isProcessing = false;
            _activePiece = null;
            _stepsRemaining = 0;
            ClearHighlights();
            ClearPieceGlows();
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
            for (float t = 0; t < 0.7f; t += 0.09f)
            {
                hudController?.ShowDiceRoll(Random.Range(diceMin, diceMax + 1));
                yield return new WaitForSeconds(0.09f);
            }
            _diceResult = Random.Range(diceMin, diceMax + 1);
            _stepsRemaining = _diceResult;
            hudController?.ShowDiceRoll(_diceResult);
            Log($"Dado: {_diceResult}");
            _isProcessing = false;
            GlowCurrentPlayerPieces();
            State = GameState.WaitingForPieceSelect;
            hudController?.SetMessage($"Dado: {_diceResult} — Elige una ficha");
        }

        private void NextTurn()
        {
            ClearPieceGlows();
            ClearHighlights();
            _activePiece = null;
            _stepsRemaining = 0;
            TurnIndex = (TurnIndex + 1) % numberOfPlayers;
            BeginTurn();
        }

        // ── Highlights ───────────────────────────────────────────────────────

        private void HighlightTiles(List<Tile> tiles)
        {
            ClearHighlights();
            foreach (Tile t in tiles) { t.SetHighlight(true); _highlightedTiles.Add(t); }
        }

        private void ClearHighlights()
        {
            foreach (Tile t in _highlightedTiles) if (t != null) t.SetHighlight(false);
            _highlightedTiles.Clear();
        }

        private void GlowCurrentPlayerPieces()
        {
            foreach (Piece p in Players[TurnIndex].pieces) p.SetSelected(true);
        }

        private void ClearPieceGlows()
        {
            foreach (PlayerData pd in Players)
                foreach (Piece p in pd.pieces) p.SetSelected(false);
        }

        // ── Setup ────────────────────────────────────────────────────────────

        private void AssignRandomElements()
        {
            Players.Clear();
            var pool = new List<ElementType>
                { ElementType.Water, ElementType.Fire, ElementType.Earth, ElementType.Air };
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            for (int p = 0; p < numberOfPlayers; p++)
            {
                Players.Add(new PlayerData
                {
                    index = p,
                    playerName = p < playerNames.Length ? playerNames[p] : $"Jugador {p + 1}",
                    element = pool[p],
                });
                Debug.Log($"[GM] {Players[p].playerName} → {Players[p].ElementName}");
            }
        }

        private void DealCards()
        {
            if (deckManager == null) deckManager = FindOrAdd<DeckManager>();
            foreach (PlayerData pd in Players) pd.hand = deckManager.DealHand(pd.element);
        }

        private void SpawnPieces()
        {
            if (piecePrefab == null) { Debug.LogError("[GM] piecePrefab null."); return; }
            foreach (PlayerData pd in Players)
            {
                Tile start = boardGenerator.GetStartTileByElement(pd.element);
                if (start == null) { Debug.LogError($"[GM] startTile null {pd.playerName}"); continue; }

                for (int i = 0; i < piecesPerPlayer; i++)
                {
                    GameObject go = Instantiate(piecePrefab);
                    go.name = $"Piece_{pd.playerName}_{i}";
                    go.transform.localScale = Vector3.one * 0.5f;

                    if (go.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.material = new Material(rend.sharedMaterial ?? rend.material);
                        rend.material.color = pd.ElementColor;
                        rend.material.EnableKeyword("_EMISSION");
                        rend.material.SetColor("_EmissionColor", Color.black);
                    }

                    Piece piece = go.GetComponent<Piece>() ?? go.AddComponent<Piece>();
                    piece.playerIndex = pd.index;
                    piece.pieceIndex = i;
                    piece.element = pd.element;
                    go.transform.position = start.transform.position + Vector3.up * 1.0f
                                          + Vector3.right * (i - 1) * 0.55f;
                    piece.currentTile = start;
                    pd.pieces.Add(piece);
                }
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private List<Piece> GetEnemiesOn(Tile tile, int myPlayer)
        {
            var result = new List<Piece>();
            foreach (PlayerData pd in Players)
            {
                if (pd.index == myPlayer) continue;
                foreach (Piece p in pd.pieces)
                {
                    Debug.Log($"  [GetEnemiesOn] check {p.name} currentTile={p.currentTile?.name} vs tile={tile?.name}");
                    if (p.currentTile == tile) result.Add(p);
                }
            }
            return result;
        }

        private List<Piece> GetAlliesOn(Tile tile, int myPlayer)
        {
            var result = new List<Piece>();
            foreach (Piece p in Players[myPlayer].pieces)
                if (p.currentTile == tile && p != _activePiece)
                    result.Add(p);
            return result;
        }

        private void AutoFindComponents()
        {
            if (boardGenerator == null) boardGenerator = Object.FindFirstObjectByType<BoardGenerator>();
            if (deckManager == null) deckManager = Object.FindFirstObjectByType<DeckManager>();
            if (hudController == null) hudController = Object.FindFirstObjectByType<HUDController>();
            if (combatManager == null) combatManager = Object.FindFirstObjectByType<CombatManager>();
        }

        private T FindOrAdd<T>() where T : Component
        {
            var c = Object.FindFirstObjectByType<T>();
            return c != null ? c : gameObject.AddComponent<T>();
        }

        public void Log(string msg)
        {
            Debug.Log($"[Game] {msg}");
            hudController?.AddToLog(msg);
        }

        private void OnGUI()
        {
            if (hudController != null) return;
            GUI.Box(new Rect(10, 10, 340, 70), "");
            GUI.Label(new Rect(16, 14, 328, 60),
                $"Turno: {CurrentPlayer?.playerName} ({CurrentPlayer?.ElementName})\n" +
                $"Estado: {State}   Dado: {_diceResult}   Pasos: {_stepsRemaining}");
            if (State == GameState.WaitingForRoll)
                if (GUI.Button(new Rect(Screen.width / 2 - 70, Screen.height - 70, 140, 44), "Lanzar dado"))
                    OnRollDice();
        }
    }
}