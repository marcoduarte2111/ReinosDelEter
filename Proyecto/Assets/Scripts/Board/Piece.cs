using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReinosDelEter
{
    public class Piece : MonoBehaviour
    {
        [Header("Identity")]
        public int playerIndex;
        public int pieceIndex;
        public ElementType element;

        [Header("Movement")]
        public float moveSpeed = 4f;
        public float hopHeight = 0.6f;

        [Header("State")]
        public Tile currentTile;
        public bool isMoving { get; private set; }

        // Callbacks
        public System.Action<Tile> OnArrivedAtTile;
        public System.Action<Tile> OnMovementFinished;
        public System.Action<Tile, int> OnReachedJunction;

        // La tile de donde vino la ficha cuando llegó al cruce
        public Tile _prevTileAtJunction { get; private set; }

        // Tile de la que vino en el último paso (para seguir recto tras un combate)
        public Tile LastStepFrom { get; private set; }

        // Pasos que quedaban cuando el movimiento se detuvo (ej. al frenar por combate)
        public int StepsLeftAtStop { get; private set; }

        // Visuals
        private Renderer _rend;
        private bool _glowing;
        private Coroutine _glowRoutine;
        private Vector3 _baseLocalPos;
        private float _bobTimer;
        private const float BobSpeed = 2.0f;
        private const float BobAmount = 0.04f;

        private void Awake() => _rend = GetComponent<Renderer>();
        private void Start() => _baseLocalPos = transform.localPosition;
        private void Update() { if (!isMoving) IdleBob(); }

        // ── Public API ───────────────────────────────────────────────────────

        public void PlaceOnTile(Tile tile)
        {
            if (tile == null) { Debug.LogError($"[Piece] PlaceOnTile null — {name}"); return; }
            currentTile = tile;
            transform.position = tile.transform.position + Vector3.up * 1.0f;
            _baseLocalPos = transform.localPosition;
        }

        /// <summary>
        /// Retorna los vecinos disponibles desde la tile actual.
        /// Excluye la tile de donde vino para evitar rebotes automáticos.
        /// </summary>
        public List<Tile> GetNeighbors(Tile cameFrom = null)
        {
            var result = new List<Tile>();
            if (currentTile == null) return result;
            foreach (Tile nb in currentTile.neighbors)
            {
                if (nb == null) continue;
                if (nb == cameFrom && currentTile.neighbors.Count > 1) continue; // evita retroceso si hay otras opciones
                result.Add(nb);
            }
            return result;
        }

        /// <summary>Inicia movimiento hacia firstStep con N pasos.</summary>
        public void StartMovement(Tile firstStep, int steps)
        {
            if (isMoving) return;
            StartCoroutine(MoveStep(firstStep, steps, currentTile));
        }

        /// <summary>Continúa movimiento (después de un cruce) sin chequear isMoving.</summary>
        public void ContinueMovement(Tile nextStep, int steps)
        {
            StopAllCoroutines();
            isMoving = false;
            stopMovement = false;
            // currentTile es el cruce actual — pasarlo como cameFrom
            // para que el primer paso no vuelva al cruce
            StartCoroutine(MoveStep(nextStep, steps, currentTile));
        }

        // Flag que GameManager activa para detener movimiento inmediatamente
        public bool stopMovement { get; set; } = false;

        // Tras ganar un combate: seguir recto por el anillo sin pedir dirección
        // en los cruces (solo el centro abre elección de camino).
        public bool continueStraight { get; set; } = false;

        // ── Movement ─────────────────────────────────────────────────────────

        private IEnumerator MoveStep(Tile target, int stepsLeft, Tile cameFrom)
        {
            isMoving = true;
            stopMovement = false;
            SetSelected(false);

            yield return StartCoroutine(HopTo(target));

            Tile prevTile = currentTile;
            currentTile = target;
            LastStepFrom = prevTile;

            OnArrivedAtTile?.Invoke(currentTile);

            if (stopMovement)
            {
                // El aterrizaje consumió un paso; guardamos lo que queda
                // para poder continuar el avance tras resolver el combate.
                StepsLeftAtStop = Mathf.Max(0, stepsLeft - 1);
                isMoving = false;
                yield break;
            }

            yield return new WaitForSeconds(0.07f);
            stepsLeft--;
            isMoving = false;

            if (stepsLeft <= 0) { OnMovementFinished?.Invoke(currentTile); yield break; }

            var forward = GetNeighborsExcluding(currentTile, prevTile);

            Debug.Log($"[MOVE] {name} | tile={currentTile.name} | prev={prevTile?.name} | neighbors={currentTile.neighbors.Count} | forward={forward.Count} | steps={stepsLeft}");
            foreach (var f in forward) Debug.Log($"  -> {f.name}");

            // Nunca avanzar automáticamente al castillo del propio elemento
            forward.RemoveAll(t => t != null
                && t.tileType == TileType.Start
                && t.pathIndex == (int)element);

            // Desde el centro siempre pide dirección
            bool forceJunction = currentTile.tileType == TileType.Center;

            if (forward.Count == 0)
                OnMovementFinished?.Invoke(currentTile);
            else if (forward.Count == 1 && !forceJunction)
                StartCoroutine(MoveStep(forward[0], stepsLeft, currentTile));
            else if (continueStraight && !forceJunction)
            {
                // Modo post-combate: no se elige camino fuera del centro.
                // Se sigue por el anillo (tiles de pathIndex -1).
                Tile straight = forward.Find(t => t.pathIndex == -1) ?? forward[0];
                StartCoroutine(MoveStep(straight, stepsLeft, currentTile));
            }
            else
            {
                _prevTileAtJunction = prevTile;
                OnReachedJunction?.Invoke(currentTile, stepsLeft);
            }
        }

        private List<Tile> GetNeighborsExcluding(Tile from, Tile exclude)
        {
            var result = new List<Tile>();
            if (from == null) return result;

            // Si solo hay 1 vecino total, no excluimos nada (para no quedarnos sin salida)
            bool hasMultiple = from.neighbors.Count > 1;

            foreach (Tile nb in from.neighbors)
            {
                if (nb == null) continue;
                if (hasMultiple && nb == exclude) continue; // excluye solo si hay más opciones
                result.Add(nb);
            }
            return result;
        }

        /// <summary>
        /// Siguiente paso en la MISMA dirección: excluye la tile de la que vino
        /// y el castillo del elemento propio. Se usa para seguir avanzando tras
        /// ganar un combate sin ofrecer la opción de retroceder.
        /// </summary>
        public Tile GetForwardStep()
        {
            var fwd = GetNeighborsExcluding(currentTile, LastStepFrom);
            fwd.RemoveAll(t => t != null
                && t.tileType == TileType.Start
                && t.pathIndex == (int)element);
            return fwd.Count > 0 ? fwd[0] : null;
        }

        private IEnumerator HopTo(Tile target)
        {
            Vector3 start = transform.position;
            Vector3 end = target.transform.position + Vector3.up * 1.0f;
            float duration = Mathf.Max(0.15f, Vector3.Distance(start, end) / moveSpeed);
            float elapsed = 0f;

            Vector3 dir = (end - start).normalized;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));

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

        public void SetSelected(bool on)
        {
            if (_glowRoutine != null) StopCoroutine(_glowRoutine);
            _glowing = on;
            if (on) _glowRoutine = StartCoroutine(GlowPulse());
            else ApplyEmission(Color.black);
        }

        private IEnumerator GlowPulse()
        {
            while (_glowing)
            {
                float b = 0.6f + Mathf.Sin(Time.time * 4f) * 0.4f;
                ApplyEmission(Color.white * b * 2f);
                yield return null;
            }
        }

        private void ApplyEmission(Color c)
        {
            if (_rend == null) return;
            _rend.material.EnableKeyword("_EMISSION");
            _rend.material.SetColor("_EmissionColor", c);
        }

        private void IdleBob()
        {
            _bobTimer += Time.deltaTime * BobSpeed;
            transform.localPosition = _baseLocalPos +
                Vector3.up * (Mathf.Sin(_bobTimer) * BobAmount);
        }

        public void PlaySelectAnimation() => StartCoroutine(ScalePop());

        private IEnumerator ScalePop()
        {
            Vector3 orig = transform.localScale;
            Vector3 big = orig * 1.4f;
            for (float t = 0; t < 0.12f; t += Time.deltaTime)
            { transform.localScale = Vector3.Lerp(orig, big, t / 0.12f); yield return null; }
            for (float t = 0; t < 0.12f; t += Time.deltaTime)
            { transform.localScale = Vector3.Lerp(big, orig, t / 0.12f); yield return null; }
            transform.localScale = orig;
        }
    }
}