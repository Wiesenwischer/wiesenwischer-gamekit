using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Camera
{
    /// <summary>
    /// Third-Person Kamera Controller.
    /// Bietet Orbit-Kontrolle um ein Ziel mit Kollisionserkennung.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Kamera-Konfiguration")]
        [SerializeField] private CameraConfig _config;

        [Header("Target")]
        [Tooltip("Das Ziel, dem die Kamera folgt")]
        [SerializeField] private Transform _target;

        [Header("Components")]
        [Tooltip("Die Unity Camera (optional - wird automatisch gesucht)")]
        [SerializeField] private UnityEngine.Camera _camera;

        // State
        private float _currentDistance;
        private float _targetDistance;
        private float _horizontalAngle;
        private float _verticalAngle;
        private Vector3 _currentVelocity;

        // Collision
        private float _collisionDistance;
        private bool _isColliding;

        // Input
        private Vector2 _inputDelta;
        private float _zoomInput;

        #region Public Properties

        /// <summary>
        /// Die Kamera-Konfiguration.
        /// </summary>
        public CameraConfig Config
        {
            get => _config;
            set => _config = value;
        }

        /// <summary>
        /// Das aktuelle Ziel.
        /// </summary>
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>
        /// Der aktuelle horizontale Winkel.
        /// </summary>
        public float HorizontalAngle => _horizontalAngle;

        /// <summary>
        /// Der aktuelle vertikale Winkel.
        /// </summary>
        public float VerticalAngle => _verticalAngle;

        /// <summary>
        /// Der aktuelle Abstand zum Ziel.
        /// </summary>
        public float CurrentDistance => _currentDistance;

        /// <summary>
        /// Ob die Kamera gerade kollidiert.
        /// </summary>
        public bool IsColliding => _isColliding;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = GetComponent<UnityEngine.Camera>();
            }

            if (_camera == null)
            {
                _camera = UnityEngine.Camera.main;
            }

            InitializeState();
        }

        private void Start()
        {
            if (_config != null && _config.LockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LateUpdate()
        {
            if (_target == null || _config == null) return;

            ProcessInput();
            UpdateRotation();
            UpdateDistance();
            CheckCollision();
            UpdatePosition();
        }

        private void OnDisable()
        {
            if (_config != null && _config.LockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        #endregion

        #region Initialization

        private void InitializeState()
        {
            if (_config == null) return;

            _targetDistance = _config.DefaultDistance;
            _currentDistance = _targetDistance;
            _collisionDistance = _targetDistance;

            // Initialisiere Winkel basierend auf aktueller Rotation
            Vector3 euler = transform.eulerAngles;
            _horizontalAngle = euler.y;
            _verticalAngle = euler.x;

            // Normalisiere vertikalen Winkel
            if (_verticalAngle > 180f)
            {
                _verticalAngle -= 360f;
            }
        }

        #endregion

        #region Input

        /// <summary>
        /// Setzt den Rotations-Input für diesen Frame.
        /// </summary>
        public void SetRotationInput(Vector2 delta)
        {
            _inputDelta = delta;
        }

        /// <summary>
        /// Setzt den Zoom-Input für diesen Frame.
        /// </summary>
        public void SetZoomInput(float zoom)
        {
            _zoomInput = zoom;
        }

        private void ProcessInput()
        {
            if (_config == null) return;

            // Process input with config settings
            Vector2 processedInput = _config.ProcessInput(_inputDelta);

            // Update angles
            _horizontalAngle += processedInput.x * _config.HorizontalSensitivity * Time.deltaTime;
            _verticalAngle -= processedInput.y * _config.VerticalSensitivity * Time.deltaTime;

            // Clamp vertical angle
            _verticalAngle = _config.ClampVerticalAngle(_verticalAngle);

            // Normalize horizontal angle
            if (_horizontalAngle > 360f) _horizontalAngle -= 360f;
            if (_horizontalAngle < 0f) _horizontalAngle += 360f;

            // Update target distance from zoom input
            _targetDistance -= _zoomInput * _config.ZoomSensitivity;
            _targetDistance = _config.ClampDistance(_targetDistance);

            // Reset input
            _inputDelta = Vector2.zero;
            _zoomInput = 0f;
        }

        #endregion

        #region Update

        private void UpdateRotation()
        {
            Quaternion targetRotation = Quaternion.Euler(_verticalAngle, _horizontalAngle, 0f);

            if (_config.RotationDamping > 0f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    1f - _config.RotationDamping
                );
            }
            else
            {
                transform.rotation = targetRotation;
            }
        }

        private void UpdateDistance()
        {
            float effectiveTargetDistance = _isColliding ? _collisionDistance : _targetDistance;

            if (_config.ZoomDamping > 0f)
            {
                float speed = _isColliding ? _config.CollisionSnapSpeed : _config.CollisionRecoverySpeed;
                _currentDistance = Mathf.Lerp(_currentDistance, effectiveTargetDistance, speed * Time.deltaTime);
            }
            else
            {
                _currentDistance = effectiveTargetDistance;
            }
        }

        private void CheckCollision()
        {
            if (_config == null || _target == null) return;

            Vector3 targetPosition = _target.position + _config.TargetOffset;
            Vector3 direction = -transform.forward;

            // SphereCast von Ziel zur Kamera
            if (Physics.SphereCast(
                targetPosition,
                _config.CollisionRadius,
                direction,
                out RaycastHit hit,
                _targetDistance,
                _config.CollisionLayers,
                QueryTriggerInteraction.Ignore))
            {
                _collisionDistance = hit.distance;
                _isColliding = true;
            }
            else
            {
                _collisionDistance = _targetDistance;
                _isColliding = false;
            }
        }

        private void UpdatePosition()
        {
            if (_target == null || _config == null) return;

            Vector3 targetPosition = _target.position + _config.TargetOffset;
            Vector3 offset = _config.ShoulderOffset;

            // Berechne Position basierend auf Rotation und Abstand
            Vector3 desiredPosition = targetPosition
                - transform.forward * _currentDistance
                + transform.right * offset.x
                + transform.up * offset.y;

            if (_config.FollowDamping > 0f)
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref _currentVelocity,
                    _config.FollowDamping
                );
            }
            else
            {
                transform.position = desiredPosition;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Setzt das Ziel der Kamera.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        /// <summary>
        /// Setzt die Kamera-Rotation direkt.
        /// </summary>
        public void SetRotation(float horizontal, float vertical)
        {
            _horizontalAngle = horizontal;
            _verticalAngle = _config != null ? _config.ClampVerticalAngle(vertical) : vertical;
        }

        /// <summary>
        /// Setzt den Abstand direkt.
        /// </summary>
        public void SetDistance(float distance)
        {
            _targetDistance = _config != null ? _config.ClampDistance(distance) : distance;
            _currentDistance = _targetDistance;
        }

        /// <summary>
        /// Richtet die Kamera hinter dem Ziel aus.
        /// </summary>
        public void SnapBehindTarget()
        {
            if (_target == null) return;

            _horizontalAngle = _target.eulerAngles.y;
            _verticalAngle = 10f; // Leicht von oben
        }

        /// <summary>
        /// Teleportiert die Kamera sofort zur Zielposition ohne Smoothing.
        /// </summary>
        public void TeleportToTarget()
        {
            if (_target == null || _config == null) return;

            _currentVelocity = Vector3.zero;
            _currentDistance = _targetDistance;

            UpdateRotation();
            CheckCollision();

            Vector3 targetPosition = _target.position + _config.TargetOffset;
            transform.position = targetPosition - transform.forward * _currentDistance;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (_target == null || _config == null) return;

            Vector3 targetPos = _target.position + _config.TargetOffset;

            // Ziel-Position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPos, 0.2f);

            // Maximaler Abstand
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPos, _config.MaxDistance);

            // Aktueller Abstand
            Gizmos.color = _isColliding ? Color.red : Color.cyan;
            Gizmos.DrawLine(targetPos, transform.position);
            Gizmos.DrawWireSphere(transform.position, _config.CollisionRadius);
        }

        #endregion
    }
}
