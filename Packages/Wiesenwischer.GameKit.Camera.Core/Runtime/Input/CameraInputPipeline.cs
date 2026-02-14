using UnityEngine;
using UnityEngine.InputSystem;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// AAA Camera Input Pipeline.
    /// Transformiert rohen Look-/Zoom-Input durch Deadzone, Acceleration
    /// und Smoothing zu einem gefilterten CameraInputState.
    /// </summary>
    public class CameraInputPipeline : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _lookActionName = "Look";
        [SerializeField] private string _zoomActionName = "ScrollWheel";

        [Header("Sensitivity")]
        [SerializeField] private float _mouseSensitivity = 0.1f;
        [SerializeField] private float _gamepadSensitivity = 3f;
        [SerializeField] private float _zoomSensitivity = 2f;

        [Header("Deadzone")]
        [Tooltip("Input unter diesem Wert wird ignoriert.")]
        [SerializeField] private float _deadzone = 0.01f;

        [Header("Acceleration")]
        [Tooltip("Exponent für Input-Beschleunigung. 1 = linear, 2 = quadratisch.")]
        [SerializeField] private float _accelerationExponent = 1.5f;

        [Header("Smoothing")]
        [Tooltip("Adaptive Smoothing-Zeit. 0 = kein Smoothing.")]
        [SerializeField] private float _smoothTime = 0.02f;

        [Header("Options")]
        [SerializeField] private bool _invertY;
        [SerializeField] private bool _lockCursor = true;

        private InputAction _lookAction;
        private InputAction _zoomAction;
        private Vector2 _smoothedLook;
        private Vector2 _smoothVelocity;
        private bool _isGamepad;

        /// <summary>Aktueller gefilterter Input-State.</summary>
        public CameraInputState CurrentInput { get; private set; }

        private void OnEnable()
        {
            if (_inputActions == null) return;

            var map = _inputActions.FindActionMap("Player");
            _lookAction = map?.FindAction(_lookActionName);
            _zoomAction = map?.FindAction(_zoomActionName);

            _lookAction?.Enable();
            _zoomAction?.Enable();

            if (_lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            _lookAction?.Disable();
            _zoomAction?.Disable();
        }

        /// <summary>
        /// Verarbeitet Input durch die Pipeline. Wird vom CameraBrain aufgerufen.
        /// </summary>
        public CameraInputState ProcessInput(float deltaTime)
        {
            Vector2 rawLook = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
            float rawZoom = _zoomAction?.ReadValue<Vector2>().y ?? 0f;

            // 1. Device Detection
            _isGamepad = IsGamepadInput();
            float sensitivity = _isGamepad ? _gamepadSensitivity : _mouseSensitivity;

            // Gamepad: Input ist pro Frame, muss mit DeltaTime skaliert werden
            // Maus: Input ist Delta, braucht kein DeltaTime
            Vector2 scaledLook = rawLook * sensitivity;
            if (_isGamepad)
                scaledLook *= deltaTime * 100f;

            if (_invertY) scaledLook.y = -scaledLook.y;

            // 2. Deadzone
            scaledLook = ApplyDeadzone(scaledLook);

            // 3. Acceleration Curve
            scaledLook = ApplyAcceleration(scaledLook);

            // 4. Adaptive Smoothing
            scaledLook = ApplySmoothing(scaledLook, deltaTime);

            float zoom = rawZoom * _zoomSensitivity;

            CurrentInput = new CameraInputState
            {
                LookX = scaledLook.x,
                LookY = scaledLook.y,
                Zoom = zoom,
                IsGamepad = _isGamepad
            };

            return CurrentInput;
        }

        private Vector2 ApplyDeadzone(Vector2 input)
        {
            if (Mathf.Abs(input.x) < _deadzone) input.x = 0f;
            if (Mathf.Abs(input.y) < _deadzone) input.y = 0f;
            return input;
        }

        private Vector2 ApplyAcceleration(Vector2 input)
        {
            float magX = Mathf.Abs(input.x);
            float magY = Mathf.Abs(input.y);

            input.x = Mathf.Sign(input.x) * Mathf.Pow(magX, _accelerationExponent);
            input.y = Mathf.Sign(input.y) * Mathf.Pow(magY, _accelerationExponent);

            return input;
        }

        private Vector2 ApplySmoothing(Vector2 input, float deltaTime)
        {
            if (_smoothTime <= 0f) return input;

            _smoothedLook.x = Mathf.SmoothDamp(
                _smoothedLook.x, input.x, ref _smoothVelocity.x, _smoothTime,
                Mathf.Infinity, deltaTime);
            _smoothedLook.y = Mathf.SmoothDamp(
                _smoothedLook.y, input.y, ref _smoothVelocity.y, _smoothTime,
                Mathf.Infinity, deltaTime);

            return _smoothedLook;
        }

        private bool IsGamepadInput()
        {
            if (_lookAction == null) return false;
            var device = _lookAction.activeControl?.device;
            return device is Gamepad;
        }
    }
}
