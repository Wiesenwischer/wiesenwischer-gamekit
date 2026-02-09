using UnityEngine;
using UnityEngine.InputSystem;

namespace Wiesenwischer.GameKit.CharacterController.Camera
{
    /// <summary>
    /// Verbindet das Unity Input System mit der ThirdPersonCamera.
    /// Arbeitet direkt mit dem InputActionAsset — ohne PlayerInput-Component.
    /// </summary>
    [RequireComponent(typeof(ThirdPersonCamera))]
    public class CameraInputHandler : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _lookActionName = "Look";
        [SerializeField] private string _zoomActionName = "ScrollWheel";

        [Header("Sensitivity")]
        [SerializeField] private float _mouseScale = 0.1f;
        [SerializeField] private float _gamepadScale = 3f;

        private ThirdPersonCamera _camera;
        private InputActionMap _actionMap;
        private InputAction _lookAction;
        private InputAction _zoomAction;
        private bool _isGamepad;

        #region Unity Callbacks

        private void Awake()
        {
            _camera = GetComponent<ThirdPersonCamera>();
        }

        private void OnEnable()
        {
            if (_inputActions == null)
            {
                Debug.LogWarning($"[CameraInputHandler] InputActionAsset fehlt auf '{gameObject.name}'!");
                return;
            }

            _actionMap = _inputActions.FindActionMap(_actionMapName);
            if (_actionMap == null)
            {
                Debug.LogError($"[CameraInputHandler] ActionMap '{_actionMapName}' nicht gefunden!");
                return;
            }

            _lookAction = _actionMap.FindAction(_lookActionName);
            _zoomAction = _actionMap.FindAction(_zoomActionName);

            _actionMap.Enable();

            InputSystem.onDeviceChange += OnDeviceChange;
            UpdateCurrentDevice();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            _actionMap?.Disable();
        }

        private void Update()
        {
            if (_camera == null) return;

            // Look
            Vector2 lookInput = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
            float scale = _isGamepad ? _gamepadScale : _mouseScale;
            _camera.SetRotationInput(lookInput * scale);

            // Zoom
            float zoomInput = 0f;
            if (_zoomAction != null)
            {
                Vector2 scrollValue = _zoomAction.ReadValue<Vector2>();
                zoomInput = scrollValue.y * 0.1f;
            }
            _camera.SetZoomInput(zoomInput);
        }

        #endregion

        #region Device Detection

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.UsageChanged)
                UpdateCurrentDevice();
        }

        private void UpdateCurrentDevice()
        {
            _isGamepad = Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame;
        }

        #endregion
    }
}
