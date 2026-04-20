using System.Collections;
using UnityEngine;

namespace ReinosDelEter
{
    /// <summary>
    /// Isometric orbit camera for the Reinos del Éter board.
    ///
    /// Controls:
    ///   Right-click drag  → orbit
    ///   Scroll wheel      → zoom
    ///   Q / E             → rotate left / right
    ///   F                 → snap back to overview
    ///
    /// Unity 6: uses Object.FindFirstObjectByType instead of deprecated FindObjectOfType.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BoardCameraController : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;
        public Vector3 targetOffset = Vector3.zero;

        [Header("Distance")]
        public float distance = 18f;
        public float minDistance = 6f;
        public float maxDistance = 35f;

        [Header("Angles")]
        public float pitch = 55f;
        public float yaw = 45f;
        public float minPitch = 20f;
        public float maxPitch = 85f;

        [Header("Speeds")]
        public float orbitSpeed = 170f;
        public float zoomSpeed = 8f;
        public float smoothTime = 0.12f;
        public float keyRotSpeed = 90f;

        private float _pitch, _yaw, _dist;
        private Vector3 _vel;

        // ── Lifecycle ────────────────────────────────────────────────────────
        private void Start()
        {
            _pitch = pitch;
            _yaw = yaw;
            _dist = distance;

            if (target == null)
            {
                // Unity 6 non-deprecated API
                var board = Object.FindFirstObjectByType<BoardGenerator>();
                target = board != null ? board.transform
                                       : new GameObject("_BoardCenter").transform;
            }

            SnapCamera();
        }

        private void LateUpdate()
        {
            HandleInput();
            SmoothApply();
        }

        // ── Input ─────────────────────────────────────────────────────────────
        private void HandleInput()
        {
            // Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            _dist = Mathf.Clamp(_dist - scroll * zoomSpeed, minDistance, maxDistance);

            // Orbit — right mouse
            if (Input.GetMouseButton(1))
            {
                _yaw += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
                _pitch -= Input.GetAxis("Mouse Y") * orbitSpeed * Time.deltaTime;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }

            // Keyboard rotate
            if (Input.GetKey(KeyCode.Q)) _yaw -= keyRotSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) _yaw += keyRotSpeed * Time.deltaTime;

            // Overview snap
            if (Input.GetKeyDown(KeyCode.F))
            {
                _pitch = pitch;
                _yaw = yaw;
                _dist = distance;
            }
        }

        // ── Camera placement ─────────────────────────────────────────────────
        private void SmoothApply()
        {
            if (target == null) return;
            Vector3 desired = DesiredPosition();
            transform.position = Vector3.SmoothDamp(
                transform.position, desired, ref _vel, smoothTime);
            transform.LookAt(target.position + targetOffset);
        }

        private void SnapCamera()
        {
            if (target == null) return;
            transform.position = DesiredPosition();
            transform.LookAt(target.position + targetOffset);
        }

        private Vector3 DesiredPosition()
        {
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            return target.position + targetOffset + rot * (Vector3.back * _dist);
        }

        // ── Focus helper ─────────────────────────────────────────────────────
        /// <summary>Briefly shifts camera focus to a world position, then returns.</summary>
        public void FocusOnPoint(Vector3 worldPos, float holdDuration = 1.5f)
            => StartCoroutine(FocusCoroutine(worldPos, holdDuration));

        private IEnumerator FocusCoroutine(Vector3 worldPos, float hold)
        {
            Vector3 orig = targetOffset;
            Vector3 shifted = worldPos - target.position;

            for (float t = 0; t < 0.4f; t += Time.deltaTime)
            {
                targetOffset = Vector3.Lerp(orig, shifted, t / 0.4f);
                yield return null;
            }

            yield return new WaitForSeconds(hold);

            for (float t = 0; t < 0.4f; t += Time.deltaTime)
            {
                targetOffset = Vector3.Lerp(shifted, orig, t / 0.4f);
                yield return null;
            }
            targetOffset = orig;
        }
    }
}