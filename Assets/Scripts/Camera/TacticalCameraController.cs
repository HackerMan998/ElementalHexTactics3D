using UnityEngine;
using UnityEngine.InputSystem;

namespace ElementalHexTactics3D.CameraControl
{
    /// <summary>
    /// Tactical 3D Orbit Camera for diorama/tactics view.
    /// Uses Unity's New Input System (UnityEngine.InputSystem).
    /// Controls:
    ///   - Q / E: Orbit rotate around focus point
    ///   - W, A, S, D / Arrows: Pan focus point across the battlefield
    ///   - Mouse Scroll: Smooth zoom in / out
    ///   - Middle Mouse Drag: Pan across terrain
    /// </summary>
    [ExecuteAlways]
    public class TacticalCameraController : MonoBehaviour
    {
        public static TacticalCameraController Instance { get; private set; }

        [Header("Focus Target")]
        [SerializeField] private Vector3 focusPoint = Vector3.zero;

        [Header("Orbit Rotation (Q / E)")]
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private bool snapToHex60Degrees = false;
        [SerializeField] private float currentYaw = 0f; // 0 = looking North (+Z) from South (-Z)
        [SerializeField] private float targetYaw = 0f;
        [SerializeField] private float yawSmoothTime = 0.15f;

        [Header("Pitch (Tilt)")]
        [SerializeField] private float pitchAngle = 50f;

        [Header("Pan Settings (WASD)")]
        [SerializeField] private float panSpeed = 12f;
        [SerializeField] private float panSmoothing = 8f;
        [SerializeField] private Vector2 panBoundsX = new Vector2(-15f, 15f);
        [SerializeField] private Vector2 panBoundsZ = new Vector2(-15f, 15f);

        [Header("Zoom Settings (Scroll Wheel)")]
        [SerializeField] private float currentDistance = 14f;
        [SerializeField] private float targetDistance = 14f;
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 25f;
        [SerializeField] private float zoomSensitivity = 0.05f;
        [SerializeField] private float zoomSmoothTime = 0.12f;

        private Vector3 targetFocusPoint;
        private float yawVelocity;
        private float zoomVelocity;
        private Vector2 lastMousePosition;
        private bool isDraggingMiddle = false;

        public Vector3 FocusPoint => focusPoint;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                if (Instance == null) Instance = this;
                else if (Instance != this) Destroy(gameObject);
            }

            targetFocusPoint = focusPoint;
            targetYaw = currentYaw;
            targetDistance = currentDistance;
        }

        private void Start()
        {
            ApplyCameraTransform();
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                HandleRotationInput();
                HandlePanInput();
                HandleZoomInput();
                HandleMouseDragPan();

                SmoothCameraMotion();
            }

            ApplyCameraTransform();
        }

        private void HandleRotationInput()
        {
            // If on Title Screen, gently auto-orbit the diorama for a stunning anime background!
            if (ElementalHexTactics3D.UI.TitleMenuManager3D.Instance != null &&
                ElementalHexTactics3D.UI.TitleMenuManager3D.Instance.IsOnTitleScreen)
            {
                targetYaw += 9f * Time.deltaTime;
                return;
            }

            if (ElementalHexTactics3D.UI.TitleMenuManager3D.Instance != null &&
                ElementalHexTactics3D.UI.TitleMenuManager3D.Instance.IsPaused)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float rotInput = 0f;
            if (keyboard.qKey.isPressed) rotInput -= 1f;
            if (keyboard.eKey.isPressed) rotInput += 1f;

            if (snapToHex60Degrees)
            {
                if (keyboard.qKey.wasPressedThisFrame) targetYaw -= 60f;
                if (keyboard.eKey.wasPressedThisFrame) targetYaw += 60f;
            }
            else
            {
                if (Mathf.Abs(rotInput) > 0.01f)
                {
                    targetYaw += rotInput * rotationSpeed * Time.deltaTime;
                }
            }
        }

        public void ResetToTacticalView()
        {
            targetFocusPoint = Vector3.zero;
            targetYaw = 0f;
            targetDistance = 14f;
        }

        private void HandlePanInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float h = 0f;
            float v = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;

            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                Quaternion camYawRotation = Quaternion.Euler(0f, currentYaw, 0f);
                Vector3 moveDir = camYawRotation * new Vector3(h, 0f, v).normalized;

                targetFocusPoint += moveDir * panSpeed * Time.deltaTime;
                targetFocusPoint.x = Mathf.Clamp(targetFocusPoint.x, panBoundsX.x, panBoundsX.y);
                targetFocusPoint.z = Mathf.Clamp(targetFocusPoint.z, panBoundsZ.x, panBoundsZ.y);
            }
        }

        private void HandleZoomInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.001f)
            {
                targetDistance -= scrollY * zoomSensitivity;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }
        }

        private void HandleMouseDragPan()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.middleButton.wasPressedThisFrame)
            {
                lastMousePosition = mouse.position.ReadValue();
                isDraggingMiddle = true;
            }
            else if (mouse.middleButton.wasReleasedThisFrame)
            {
                isDraggingMiddle = false;
            }
            else if (isDraggingMiddle && mouse.middleButton.isPressed)
            {
                Vector2 currentPos = mouse.position.ReadValue();
                Vector2 delta = currentPos - lastMousePosition;
                lastMousePosition = currentPos;

                Quaternion camYawRotation = Quaternion.Euler(0f, currentYaw, 0f);
                Vector3 move = camYawRotation * new Vector3(-delta.x, 0f, -delta.y) * 0.015f;

                targetFocusPoint += move;
                targetFocusPoint.x = Mathf.Clamp(targetFocusPoint.x, panBoundsX.x, panBoundsX.y);
                targetFocusPoint.z = Mathf.Clamp(targetFocusPoint.z, panBoundsZ.x, panBoundsZ.y);
            }
        }

        [Header("Screen Shake Juice")]
        private float shakeDuration = 0f;
        private float shakeTotalDuration = 0.1f;
        private float shakeIntensity = 0f;

        private void SmoothCameraMotion()
        {
            focusPoint = Vector3.Lerp(focusPoint, targetFocusPoint, Time.deltaTime * panSmoothing);
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, yawSmoothTime);
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);
        }

        public void ApplyCameraTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitchAngle, currentYaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -currentDistance);

            Vector3 shakeOffset = Vector3.zero;
            if (Application.isPlaying && shakeDuration > 0f)
            {
                shakeDuration -= Time.deltaTime;
                float progress = Mathf.Clamp01(shakeDuration / Mathf.Max(0.001f, shakeTotalDuration));
                float currentMag = shakeIntensity * progress;
                shakeOffset = Random.insideUnitSphere * currentMag;
            }

            transform.position = focusPoint + offset + shakeOffset;
            transform.rotation = rotation;
        }

        /// <summary>
        /// Triggers a dynamic trauma screen shake that smoothly decays.
        /// </summary>
        public void Shake(float duration = 0.35f, float intensity = 0.35f)
        {
            shakeDuration = duration;
            shakeTotalDuration = Mathf.Max(0.01f, duration);
            shakeIntensity = intensity;
        }

        public void FocusOnPosition(Vector3 position)
        {
            targetFocusPoint = new Vector3(position.x, 0f, position.z);
        }

        private void OnValidate()
        {
            targetYaw = currentYaw;
            targetDistance = currentDistance;
            targetFocusPoint = focusPoint;
            ApplyCameraTransform();
        }
    }
}
