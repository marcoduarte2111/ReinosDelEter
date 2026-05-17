using System.Collections;
using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// DiceRoller — dado 3D con física y animación.
    ///
    /// SETUP EN UNITY:
    ///   1. Crea un cubo 3D (GameObject → 3D Object → Cube)
    ///   2. Nómbralo "Dice"
    ///   3. Agrégale Rigidbody (Add Component → Rigidbody)
    ///   4. Agrégale este script
    ///   5. Crea un material para cada cara (1-6) y asígnalos en diceFaceMaterials[]
    ///      O usa un solo material con textura de dado
    ///   6. Coloca el dado en la escena en una posición visible (ej. X=5, Y=3, Z=0)
    ///   7. Crea un plano debajo del dado (GameObject → 3D Object → Plane) como superficie
    ///   8. Asigna el dado al campo "diceRoller" en el HUDController o GameManager
    ///
    /// CÓMO FUNCIONA:
    ///   • Al llamar RollDice(), el dado salta con física real
    ///   • Cuando se detiene, detecta qué cara está mirando hacia arriba
    ///   • Llama onRollComplete(result) con el número 1-6
    /// </summary>
    public class DiceRoller : MonoBehaviour
    {
        [Header("Referencias")]
        public Rigidbody diceRigidbody;

        [Header("Física del lanzamiento")]
        public float throwForce = 8f;    // fuerza hacia arriba
        public float torqueForce = 12f;   // fuerza de rotación
        public float settleTime = 2.5f;  // tiempo máximo esperando que se detenga
        public float velocityThreshold = 0.05f; // velocidad mínima para considerar detenido

        [Header("Posición de reposo")]
        public Vector3 restPosition = new Vector3(0f, 0.6f, 11f);
        public Vector3 throwPosition = new Vector3(0f, 2.5f, 11f);

        [Header("Límites del área de lanzamiento")]
        [Tooltip("Centro del área (pon las mismas X,Z que el plano)")]
        public Vector3 areaCenter = new Vector3(0f, 0f, 11f);
        [Tooltip("Mitad del ancho y largo del área")]
        public Vector2 areaSize = new Vector2(1.5f, 1.5f);

        [Header("Materiales de caras (opcional)")]
        [Tooltip("6 materiales, uno por cara. Déjalos vacíos si usas textura atlas.")]
        public Material[] diceFaceMaterials; // índice 0=cara1 ... 5=cara6

        // Callback cuando termina el lanzamiento
        public System.Action<int> onRollComplete;

        private bool _isRolling = false;
        public bool IsRolling => _isRolling;

        private void Awake()
        {
            if (diceRigidbody == null)
                diceRigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (!_isRolling) return;

            // Clampea la posición dentro del área definida
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, areaCenter.x - areaSize.x, areaCenter.x + areaSize.x);
            pos.z = Mathf.Clamp(pos.z, areaCenter.z - areaSize.y, areaCenter.z + areaSize.y);
            transform.position = pos;

            // Si el dado sale del área también frena la velocidad horizontal
            Vector3 vel = diceRigidbody.linearVelocity;
            if (transform.position.x <= areaCenter.x - areaSize.x ||
                transform.position.x >= areaCenter.x + areaSize.x)
                vel.x = -vel.x * 0.5f;
            if (transform.position.z <= areaCenter.z - areaSize.y ||
                transform.position.z >= areaCenter.z + areaSize.y)
                vel.z = -vel.z * 0.5f;
            diceRigidbody.linearVelocity = vel;
        }

        /// <summary>Lanza el dado y llama onRollComplete(1-6) cuando termina.</summary>
        public void RollDice()
        {
            if (_isRolling) return;
            StartCoroutine(RollCoroutine());
        }

        private IEnumerator RollCoroutine()
        {
            _isRolling = true;

            // Primero desactiva kinematic, LUEGO resetea velocidades
            diceRigidbody.isKinematic = false;
            diceRigidbody.linearVelocity = Vector3.zero;
            diceRigidbody.angularVelocity = Vector3.zero;

            transform.position = throwPosition;
            transform.rotation = Random.rotation;

            // Aplica fuerza hacia abajo + torque aleatorio
            Vector3 force = new Vector3(
                Random.Range(-1f, 1f),
                -2f,
                Random.Range(-1f, 1f)
            );
            Vector3 torque = new Vector3(
                Random.Range(-torqueForce, torqueForce),
                Random.Range(-torqueForce, torqueForce),
                Random.Range(-torqueForce, torqueForce)
            );

            diceRigidbody.AddForce(force, ForceMode.Impulse);
            diceRigidbody.AddTorque(torque, ForceMode.Impulse);

            // Espera a que se detenga (máx settleTime segundos)
            float elapsed = 0f;
            yield return new WaitForSeconds(0.5f); // mínimo de animación

            while (elapsed < settleTime)
            {
                if (diceRigidbody.linearVelocity.magnitude < velocityThreshold &&
                    diceRigidbody.angularVelocity.magnitude < velocityThreshold)
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Fija el dado
            diceRigidbody.linearVelocity = Vector3.zero;
            diceRigidbody.angularVelocity = Vector3.zero;
            diceRigidbody.isKinematic = true;

            // Detecta el resultado
            int result = GetTopFace();

            // Snap a posición de reposo limpia
            yield return StartCoroutine(SnapToRest(result));

            _isRolling = false;
            onRollComplete?.Invoke(result);
        }

        /// <summary>
        /// Detecta qué cara del dado está mirando hacia arriba.
        /// Basado en los ejes locales del cubo:
        ///   +Y local = cara 1,  -Y = cara 6
        ///   +X local = cara 2,  -X = cara 5
        ///   +Z local = cara 3,  -Z = cara 4
        /// Ajusta este mapeo según la textura de tu dado.
        /// </summary>
        private int GetTopFace()
        {
            Vector3 up = Vector3.up;

            Vector3[] axes = {
                transform.up,       // +Y
                -transform.up,      // -Y
                transform.right,    // +X
                -transform.right,   // -X
                transform.forward,  // +Z
                -transform.forward  // -Z
            };
            // +Y=2, -Y=5, +X=4, -X=3, +Z=1, -Z=6
            int[] faceValues = { 2, 5, 4, 3, 1, 6 };

            float maxDot = -1f;
            int topFace = 1;

            for (int i = 0; i < axes.Length; i++)
            {
                float dot = Vector3.Dot(axes[i], up);
                if (dot > maxDot) { maxDot = dot; topFace = faceValues[i]; }
            }

            return topFace;
        }

        private Quaternion GetRotationForFace(int face) => face switch
        {
            2 => Quaternion.Euler(0f, 0f, 0f),
            5 => Quaternion.Euler(180f, 0f, 0f),
            3 => Quaternion.Euler(0f, 0f, -90f),
            4 => Quaternion.Euler(0f, 0f, 90f),
            1 => Quaternion.Euler(90f, 0f, 0f),
            6 => Quaternion.Euler(-90f, 0f, 0f),
            _ => Quaternion.identity
        };

        private IEnumerator SnapToRest(int result)
        {
            // Pausa para que el jugador vea el resultado
            yield return new WaitForSeconds(1.2f);

            Quaternion targetRot = GetRotationForFace(result);
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float duration = 0.4f;

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float p = t / duration;
                transform.position = Vector3.Lerp(startPos, restPosition, p);
                transform.rotation = Quaternion.Lerp(startRot, targetRot, p);
                yield return null;
            }

            transform.position = restPosition;
            transform.rotation = targetRot;
        }

        /// <summary>
        /// Asigna los 6 materiales a las caras del MeshRenderer.
        /// El cubo de Unity tiene 1 material — para 6 caras distintas necesitas
        /// usar submeshes o una textura atlas.
        /// Este método es útil si usas un modelo de dado con 6 submeshes.
        /// </summary>
        public void ApplyFaceMaterials()
        {
            if (diceFaceMaterials == null || diceFaceMaterials.Length < 6) return;
            var mr = GetComponent<MeshRenderer>();
            if (mr == null) return;
            mr.materials = diceFaceMaterials;
        }
    }
}