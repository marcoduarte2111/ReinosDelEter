using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// One of the 3 pieces each player controls.
    /// Handles smooth hop-to-tile movement and idle bob.
    ///
    /// Unity 6: no deprecated API used.
    /// </summary>
    public class Piece : MonoBehaviour
    {
        [Header("Identity")]
        public int playerIndex;   // 0-3
        public int pieceIndex;    // 0-2
        public ElementType element;

        [Header("Movement")]
        public float moveSpeed = 4f;
        public float hopHeight = 0.5f;

        [Header("State (read-only)")]
        public Tile currentTile;
        public bool isMoving { get; private set; }

        // Callbacks
        public System.Action<Tile> OnArrivedAtTile;
        public System.Action<Tile> OnMovementFinished;

        // Idle bob
        private Vector3 _baseLocalPos;
        private float _bobTimer;
        private const float BobSpeed = 2.2f;
        private const float BobAmount = 0.05f;

        // ── Lifecycle ────────────────────────────────────────────────────────
        private void Start() => _baseLocalPos = transform.localPosition;

        private void Update()
        {
            if (!isMoving) IdleBob();
        }

        // ── Public API ───────────────────────────────────────────────────────
        /// <summary>Teleport piece to tile without animation.</summary>
        public void PlaceOnTile(Tile tile)
        {
            if (tile == null) { Debug.LogError($"[Piece] PlaceOnTile: tile es null para {name}"); return; }
            currentTile = tile;
            transform.position = tile.transform.position + Vector3.up * 1.2f;
            _baseLocalPos = transform.localPosition;
        }

        /// <summary>Move piece forward N steps, usando la dirección elegida.</summary>
        public void MoveSteps(int steps)
        {
            if (isMoving || currentTile == null) return;
            StartCoroutine(MoveCoroutine(steps));
        }

        /// <summary>Mueve la ficha con una dirección inicial elegida por el jugador.</summary>
        public void MoveStepsWithDirection(int steps, Tile firstStep)
        {
            if (isMoving || currentTile == null) return;
            StartCoroutine(MoveCoroutineWithFirstStep(steps, firstStep));
        }

        /// <summary>Retorna todas las tiles a las que puede ir desde la posición actual.</summary>
        public List<Tile> GetAvailableDirections()
        {
            List<Tile> options = new List<Tile>();
            if (currentTile == null) return options;

            // Si tiene connectedTiles (esquina), esas son las opciones
            if (currentTile.connectedTiles != null && currentTile.connectedTiles.Length > 0)
            {
                foreach (Tile t in currentTile.connectedTiles)
                    if (t != null) options.Add(t);
                return options;
            }

            // Si no, solo nextTile
            if (currentTile.nextTile != null)
                options.Add(currentTile.nextTile);

            return options;
        }

        // ── Movement ─────────────────────────────────────────────────────────
        private IEnumerator MoveCoroutine(int steps)
        {
            isMoving = true;
            for (int s = 0; s < steps; s++)
            {
                Tile next = currentTile?.nextTile;
                if (next == null) break;
                yield return StartCoroutine(HopTo(next));
                currentTile = next;
                OnArrivedAtTile?.Invoke(currentTile);
                yield return new WaitForSeconds(0.08f);
            }
            isMoving = false;
            OnMovementFinished?.Invoke(currentTile);
        }

        private IEnumerator MoveCoroutineWithFirstStep(int steps, Tile firstStep)
        {
            isMoving = true;

            // Primer paso: dirección elegida
            yield return StartCoroutine(HopTo(firstStep));
            currentTile = firstStep;
            OnArrivedAtTile?.Invoke(currentTile);
            yield return new WaitForSeconds(0.08f);

            // Pasos restantes: sigue nextTile normalmente
            for (int s = 1; s < steps; s++)
            {
                Tile next = currentTile?.nextTile;
                if (next == null) break;
                yield return StartCoroutine(HopTo(next));
                currentTile = next;
                OnArrivedAtTile?.Invoke(currentTile);
                yield return new WaitForSeconds(0.08f);
            }

            isMoving = false;
            OnMovementFinished?.Invoke(currentTile);
        }

        private IEnumerator HopTo(Tile target)
        {
            Vector3 start = transform.position;
            Vector3 end = target.transform.position + Vector3.up * 0.5f;
            float dist = Vector3.Distance(start, end);
            float duration = dist / moveSpeed;
            float elapsed = 0f;

            // Face movement direction
            Vector3 dir = (end - start).normalized;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float arc = Mathf.Sin(t * Mathf.PI) * hopHeight;
                transform.position = Vector3.Lerp(start, end, t) + Vector3.up * arc;
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = end;
            _baseLocalPos = transform.localPosition;
            target.PlayLandEffect();
        }

        // ── Visuals ──────────────────────────────────────────────────────────
        private void IdleBob()
        {
            _bobTimer += Time.deltaTime * BobSpeed;
            transform.localPosition = _baseLocalPos + Vector3.up * (Mathf.Sin(_bobTimer) * BobAmount);
        }

        public void PlaySelectAnimation() => StartCoroutine(ScalePop());

        private IEnumerator ScalePop()
        {
            Vector3 orig = transform.localScale;
            Vector3 big = orig * 1.35f;

            for (float t = 0; t < 0.15f; t += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(orig, big, t / 0.15f);
                yield return null;
            }
            for (float t = 0; t < 0.15f; t += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(big, orig, t / 0.15f);
                yield return null;
            }
            transform.localScale = orig;
        }
    }
}