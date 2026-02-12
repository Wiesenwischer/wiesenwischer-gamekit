using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.IK
{
    /// <summary>
    /// Standard IIKTargetProvider: Nutzt die Hauptkamera als LookAt-Ziel.
    /// Der Blickpunkt ist Camera.main.transform.position + forward * distance.
    /// </summary>
    public class CameraTargetProvider : MonoBehaviour, IIKTargetProvider
    {
        [Tooltip("Entfernung des LookAt-Ziels vor der Kamera.")]
        [SerializeField] private float _lookDistance = 10f;

        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
        }

        public Vector3 GetLookTarget()
        {
            if (_camera == null) return transform.position + transform.forward;
            return _camera.transform.position + _camera.transform.forward * _lookDistance;
        }

        public bool HasLookTarget => _camera != null;
    }
}
