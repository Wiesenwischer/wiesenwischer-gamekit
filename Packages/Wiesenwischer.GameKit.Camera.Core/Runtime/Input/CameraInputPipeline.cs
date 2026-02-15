using UnityEngine;
using UnityEngine.InputSystem;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// AAA Camera Input Pipeline.
    /// Transformiert rohen Look-/Zoom-Input durch Deadzone, Acceleration
    /// und Smoothing zu einem gefilterten CameraInputState.
    /// Unterstützt AlwaysOn (BDO) und ButtonActivated (ArcheAge/WoW) Orbit-Modi.
    /// </summary>
    public class CameraInputPipeline : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _lookActionName = "Look";
        [SerializeField] private string _zoomActionName = "ScrollWheel";

        [Header("Orbit Activation")]
        [Tooltip("AlwaysOn = Maus steuert immer Kamera (BDO). ButtonActivated = Nur bei LMB/RMB (ArcheAge).")]
        [SerializeField] private OrbitActivation _orbitActivation = OrbitActivation.AlwaysOn;

        [Header("Orbit Buttons (nur bei ButtonActivated)")]
        [Tooltip("Input Action für Free Look (Kamera rotiert, Character nicht). Standard: LMB.")]
        [SerializeField] private string _freeLookButtonName = "FreeLook";

        [Tooltip("Input Action für Steer (Kamera + Character drehen). Standard: RMB.")]
        [SerializeField] private string _steerButtonName = "Steer";

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

        private InputAction _lookAction;
        private InputAction _zoomAction;
        private InputAction _freeLookAction;
        private InputAction _steerAction;
        private Vector2 _smoothedLook;
        private Vector2 _smoothVelocity;
        private bool _isGamepad;

        /// <summary>Aktueller gefilterter Input-State.</summary>
        public CameraInputState CurrentInput { get; private set; }

        /// <summary>Aktueller OrbitActivation-Modus. Kann von CameraBrain gesetzt werden.</summary>
        public OrbitActivation OrbitActivationMode
        {
            get => _orbitActivation;
            set
            {
                if (_orbitActivation == value) return;
                _orbitActivation = value;
                UpdateCursorState(CameraOrbitMode.None);
            }
        }

        private void OnEnable()
        {
            if (_inputActions == null) return;

            var map = _inputActions.FindActionMap("Player");
            _lookAction = map?.FindAction(_lookActionName);
            _zoomAction = map?.FindAction(_zoomActionName);
            _freeLookAction = map?.FindAction(_freeLookButtonName);
            _steerAction = map?.FindAction(_steerButtonName);

            _lookAction?.Enable();
            _zoomAction?.Enable();
            _freeLookAction?.Enable();
            _steerAction?.Enable();

            // Initial Cursor State
            if (_orbitActivation == OrbitActivation.AlwaysOn)
                UpdateCursorState(CameraOrbitMode.FreeOrbit);
            else
                UpdateCursorState(CameraOrbitMode.None);
        }

        private void OnDisable()
        {
            _lookAction?.Disable();
            _zoomAction?.Disable();
            _freeLookAction?.Disable();
            _steerAction?.Disable();

            // Cursor freigeben
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Verarbeitet Input durch die Pipeline. Wird vom CameraBrain aufgerufen.
        /// </summary>
        public CameraInputState ProcessInput(float deltaTime)
        {
            // 0. Orbit Mode bestimmen
            CameraOrbitMode orbitMode = DetermineOrbitMode();
            UpdateCursorState(orbitMode);

            Vector2 rawLook = Vector2.zero;
            float rawZoom = _zoomAction?.ReadValue<Vector2>().y ?? 0f;

            // Look-Input nur lesen wenn Orbit aktiv
            if (orbitMode != CameraOrbitMode.None)
                rawLook = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

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
            if (orbitMode != CameraOrbitMode.None)
            {
                scaledLook = ApplySmoothing(scaledLook, deltaTime);
            }
            else
            {
                // Reset smoothing wenn kein Orbit → kein Carry-Over
                _smoothedLook = Vector2.zero;
                _smoothVelocity = Vector2.zero;
            }

            float zoom = rawZoom * _zoomSensitivity;

            CurrentInput = new CameraInputState
            {
                LookX = scaledLook.x,
                LookY = scaledLook.y,
                Zoom = zoom,
                IsGamepad = _isGamepad,
                OrbitMode = orbitMode
            };

            return CurrentInput;
        }

        private CameraOrbitMode DetermineOrbitMode()
        {
            if (_orbitActivation == OrbitActivation.AlwaysOn)
            {
                // BDO-Style: Immer FreeOrbit (Character wird nie von Kamera gesteuert)
                return CameraOrbitMode.FreeOrbit;
            }

            // ButtonActivated (ArcheAge/WoW-Style)
            // Gamepad: Rechter Stick = immer FreeOrbit
            if (_isGamepad)
                return CameraOrbitMode.FreeOrbit;

            bool steerHeld = _steerAction?.IsPressed() ?? false;
            bool freeLookHeld = _freeLookAction?.IsPressed() ?? false;

            // Steer hat Priorität (wenn beide gedrückt → Steer)
            if (steerHeld)
                return CameraOrbitMode.SteerOrbit;
            if (freeLookHeld)
                return CameraOrbitMode.FreeOrbit;

            return CameraOrbitMode.None;
        }

        private void UpdateCursorState(CameraOrbitMode mode)
        {
            if (mode != CameraOrbitMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
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
