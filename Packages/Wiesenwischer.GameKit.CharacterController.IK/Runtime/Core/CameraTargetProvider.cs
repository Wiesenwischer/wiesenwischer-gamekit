using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.CharacterController.IK
{
    /// <summary>
    /// Standard IIKTargetProvider: Nutzt die Hauptkamera als LookAt-Ziel.
    /// Im Idle schaut der Character zur Kamera-Position.
    /// In Bewegung schaut er in die Kamera-Blickrichtung (forward * distance).
    /// </summary>
    public class CameraTargetProvider : MonoBehaviour, IIKTargetProvider
    {
        [Tooltip("Entfernung des LookAt-Ziels vor der Kamera (während Bewegung).")]
        [SerializeField] private float _lookDistance = 10f;

        [Tooltip("Referenz zum PlayerController für Idle-Erkennung.")]
        [SerializeField] private PlayerController _playerController;

        [Tooltip("Geschwindigkeit unter der als Idle gilt.")]
        [SerializeField] private float _idleThreshold = 0.1f;

        [Tooltip("Wie schnell zwischen Idle/Moving Target gewechselt wird.")]
        [SerializeField] private float _blendSpeed = 3f;

        private Camera _camera;
        private float _movingBlend; // 0 = idle (look at camera), 1 = moving (look forward)

        private void Awake()
        {
            // Camera.main wird NICHT in Awake() gecached, weil PivotRig.EnsureHierarchy()
            // die Camera erst in CameraBrain.Awake([DefaultExecutionOrder(100)]) migriert.
            // Stattdessen: Lazy-Resolution in ResolveCamera().
            if (_playerController == null)
                _playerController = GetComponentInParent<PlayerController>();
        }

        private Camera ResolveCamera()
        {
            if (_camera != null && _camera.enabled)
                return _camera;
            _camera = Camera.main;
            return _camera;
        }

        public Vector3 GetLookTarget()
        {
            var cam = ResolveCamera();
            if (cam == null) return transform.position + transform.forward;

            float speed = _playerController != null
                ? new Vector3(_playerController.Velocity.x, 0f, _playerController.Velocity.z).magnitude
                : 0f;
            float targetBlend = speed > _idleThreshold ? 1f : 0f;
            _movingBlend = Mathf.MoveTowards(_movingBlend, targetBlend, _blendSpeed * Time.deltaTime);

            Vector3 idleTarget = cam.transform.position;
            Vector3 movingTarget = cam.transform.position + cam.transform.forward * _lookDistance;

            return Vector3.Lerp(idleTarget, movingTarget, _movingBlend);
        }

        public bool HasLookTarget => ResolveCamera() != null;
    }
}
