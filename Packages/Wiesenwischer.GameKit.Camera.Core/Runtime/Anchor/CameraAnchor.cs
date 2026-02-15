using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Stabilisiert die Follow-Position gegen Animation Noise und Root Motion Jitter.
    /// Folgt dem Movement Root Transform mit konfigurierbaren SmoothDamp-Werten.
    /// </summary>
    public class CameraAnchor : MonoBehaviour
    {
        [Header("Follow Target")]
        [SerializeField] private Transform _followTarget;

        [Header("Smoothing")]
        [Tooltip("SmoothDamp-Zeit für horizontale Bewegung (XZ). Niedrig = wenig Lag.")]
        [SerializeField] private float _horizontalDamping = 0.02f;

        [Tooltip("SmoothDamp-Zeit für vertikale Bewegung (Y). Höher = stärkere Glättung.")]
        [SerializeField] private float _verticalDamping = 0.08f;

        [Header("Target Offset")]
        [Tooltip("Offset relativ zum Follow-Target (z.B. Höhe über Character-Root).")]
        [SerializeField] private Vector3 _targetOffset = new Vector3(0f, 1.5f, 0f);

        private Vector3 _velocityXZ;
        private float _velocityY;

        /// <summary>Aktuelle stabilisierte Position.</summary>
        public Vector3 AnchorPosition { get; private set; }

        public Transform FollowTarget
        {
            get => _followTarget;
            set => _followTarget = value;
        }

        public Vector3 TargetOffset
        {
            get => _targetOffset;
            set => _targetOffset = value;
        }

        /// <summary>
        /// Aktualisiert die Anchor-Position. Wird vom CameraBrain aufgerufen.
        /// </summary>
        public void UpdateAnchor(float deltaTime)
        {
            if (_followTarget == null) return;

            Vector3 targetPos = _followTarget.position + _targetOffset;
            Vector3 current = AnchorPosition;

            float smoothedX = Mathf.SmoothDamp(current.x, targetPos.x, ref _velocityXZ.x, _horizontalDamping, Mathf.Infinity, deltaTime);
            float smoothedZ = Mathf.SmoothDamp(current.z, targetPos.z, ref _velocityXZ.z, _horizontalDamping, Mathf.Infinity, deltaTime);
            float smoothedY = Mathf.SmoothDamp(current.y, targetPos.y, ref _velocityY, _verticalDamping, Mathf.Infinity, deltaTime);

            AnchorPosition = new Vector3(smoothedX, smoothedY, smoothedZ);
        }

        /// <summary>
        /// Teleportiert den Anchor sofort zur Zielposition (kein Smoothing).
        /// </summary>
        public void SnapToTarget()
        {
            if (_followTarget == null) return;
            AnchorPosition = _followTarget.position + _targetOffset;
            _velocityXZ = Vector3.zero;
            _velocityY = 0f;
        }
    }
}
