using System.Collections.Generic;
using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// MovementDebugger — presiona M en Play para ver estado completo de todas las fichas.
    /// También loguea automáticamente cada evento de movimiento y combate.
    /// </summary>
    public class MovementDebugger : MonoBehaviour
    {
        private GameManager _gm;
        private List<string> _eventLog = new();
        private const int MaxLog = 30;

        private void Start()
        {
            _gm = GameManager.Instance;
            if (_gm == null) { Debug.LogError("[MovDebug] No GameManager"); return; }

            // Suscribe a eventos de movimiento de todas las fichas
            StartCoroutine(WaitAndSubscribe());
        }

        private System.Collections.IEnumerator WaitAndSubscribe()
        {
            yield return new WaitForSeconds(1f); // espera a que spawnen
            SubscribeToAllPieces();
        }

        private void SubscribeToAllPieces()
        {
            if (_gm?.Players == null) return;
            foreach (var pd in _gm.Players)
                foreach (var piece in pd.pieces)
                    SubscribePiece(piece, pd);
            Log("=== MovementDebugger suscrito a todas las fichas ===");
        }

        private void SubscribePiece(Piece piece, PlayerData pd)
        {
            // No sobreescribimos los callbacks del GameManager
            // En su lugar usamos un wrapper que loguea y luego llama el original
            var origArrived = piece.OnArrivedAtTile;
            var origFinished = piece.OnMovementFinished;
            var origJunction = piece.OnReachedJunction;

            piece.OnArrivedAtTile = tile =>
            {
                Log($"[STEP] {piece.name} ({pd.ElementName}) → {tile?.name ?? "NULL"} " +
                    $"(nextTile={tile?.nextTile?.name ?? "NULL"} " +
                    $"connectedTiles={tile?.connectedTiles?.Length ?? 0})");
                origArrived?.Invoke(tile);
            };

            piece.OnMovementFinished = tile =>
            {
                Log($"[DONE] {piece.name} terminó en {tile?.name ?? "NULL"}");
                origFinished?.Invoke(tile);
            };

            piece.OnReachedJunction = (tile, stepsLeft) =>
            {
                Log($"[JUNCTION] {piece.name} en cruce {tile?.name ?? "NULL"} — {stepsLeft} pasos restantes");
                if (tile?.connectedTiles != null)
                    foreach (var t in tile.connectedTiles)
                        Log($"  opción → {t?.name ?? "NULL"}");
                origJunction?.Invoke(tile, stepsLeft);
            };
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) PrintFullState();
            if (Input.GetKeyDown(KeyCode.R)) SubscribeToAllPieces(); // re-suscribe si cambiaron
        }

        private void PrintFullState()
        {
            if (_gm?.Players == null) { Debug.Log("[MovDebug] Sin jugadores"); return; }

            Debug.Log("════════════════ ESTADO DE FICHAS ════════════════");
            Debug.Log($"Turno: Jugador {_gm.TurnIndex + 1} | Estado: {_gm.State}");

            foreach (var pd in _gm.Players)
            {
                Debug.Log($"── {pd.playerName} ({pd.ElementName}) HP={pd.health} Score={pd.score}");
                foreach (var piece in pd.pieces)
                {
                    Tile ct = piece.currentTile;
                    Debug.Log($"   {piece.name}: " +
                              $"tile={ct?.name ?? "NULL"} " +
                              $"isMoving={piece.isMoving} " +
                              $"nextTile={ct?.nextTile?.name ?? "NULL"} " +
                              $"connectedTiles={ct?.connectedTiles?.Length ?? 0}");
                }
            }
            Debug.Log("════════════════════════════════════════════════");
        }

        private void Log(string msg)
        {
            Debug.Log($"[MovDebug] {msg}");
            _eventLog.Add($"[{System.DateTime.Now:HH:mm:ss}] {msg}");
            if (_eventLog.Count > MaxLog) _eventLog.RemoveAt(0);
        }

        private void OnGUI()
        {
            // Mini-log en pantalla (esquina inferior izquierda)
            GUI.Box(new Rect(0, Screen.height - 200, 420, 200), "");
            string display = string.Join("\n", _eventLog.GetRange(
                Mathf.Max(0, _eventLog.Count - 8), Mathf.Min(8, _eventLog.Count)));
            GUI.Label(new Rect(4, Screen.height - 196, 412, 192), display);
            GUI.Label(new Rect(4, Screen.height - 4, 300, 16),
                "M = estado fichas | R = re-suscribir");
        }
    }
}
