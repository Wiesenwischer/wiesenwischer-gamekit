using UnityEngine;
using UnityEngine.InputSystem;

namespace Wiesenwischer.GameKit.CharacterController.Camera
{
    /// <summary>
    /// Verbindet das Unity Input System mit der ThirdPersonCamera.
    /// </summary>
    [RequireComponent(typeof(ThirdPersonCamera))]
    public class CameraInputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        [Tooltip("Input Action für Kamera-Rotation (Look)")]
        [SerializeField] private InputActionReference _lookAction;

        [Tooltip("Input Action für Zoom (ScrollWheel)")]
        [SerializeField] private InputActionReference _zoomAction;

        [Header("Mouse Settings")]
        [Tooltip("Skalierung für Maus-Input")]
        [SerializeField] private float _mouseScale = 0.1f;

        [Tooltip("Skalierung für Gamepad-Input")]
        [SerializeField] private float _gamepadScale = 3f;

        private ThirdPersonCamera _camera;
        private PlayerInput _playerInput;
        private InputAction _lookInputAction;
        private InputAction _zoomInputAction;
        private bool _isGamepad;

        #region Unity Callbacks

        private void Awake()
        {
            _camera = GetComponent<ThirdPersonCamera>();
        }

        private void OnEnable()
        {
            SetupInputActions();
        }

        private void OnDisable()
        {
            CleanupInputActions();
        }

        private void Update()
        {
            ReadInput();
        }

        #endregion

        #region Input Setup

        private void SetupInputActions()
        {
            // Versuche InputActions von Reference zu bekommen
            if (_lookAction != null)
            {
                _lookInputAction = _lookAction.action;
                _lookInputAction?.Enable();
            }

            if (_zoomAction != null)
            {
                _zoomInputAction = _zoomAction.action;
                _zoomInputAction?.Enable();
            }

            // Fallback: Suche PlayerInput Component
            if (_lookInputAction == null)
            {
                _playerInput = FindObjectOfType<PlayerInput>();
                if (_playerInput != null)
                {
                    _lookInputAction = _playerInput.actions?.FindAction("Look");
                    _zoomInputAction = _playerInput.actions?.FindAction("ScrollWheel");
                }
            }

            // Registriere für Device-Changes
            InputSystem.onDeviceChange += OnDeviceChange;
            UpdateCurrentDevice();
        }

        private void CleanupInputActions()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.UsageChanged)
            {
                UpdateCurrentDevice();
            }
        }

        private void UpdateCurrentDevice()
        {
            // Prüfe ob Gamepad aktiv ist
            _isGamepad = Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame;
        }

        #endregion

        #region Input Reading

        private void ReadInput()
        {
            if (_camera == null) return;

            // Look Input
            Vector2 lookInput = Vector2.zero;
            if (_lookInputAction != null)
            {
                lookInput = _lookInputAction.ReadValue<Vector2>();
            }

            // Skaliere Input je nach Device
            float scale = _isGamepad ? _gamepadScale : _mouseScale;
            lookInput *= scale;

            _camera.SetRotationInput(lookInput);

            // Zoom Input
            float zoomInput = 0f;
            if (_zoomInputAction != null)
            {
                Vector2 scrollValue = _zoomInputAction.ReadValue<Vector2>();
                zoomInput = scrollValue.y * 0.1f; // Normalisiere Scroll-Werte
            }

            _camera.SetZoomInput(zoomInput);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Setzt die Look-Action manuell.
        /// </summary>
        public void SetLookAction(InputAction action)
        {
            _lookInputAction = action;
        }

        /// <summary>
        /// Setzt die Zoom-Action manuell.
        /// </summary>
        public void SetZoomAction(InputAction action)
        {
            _zoomInputAction = action;
        }

        #endregion
    }
}
