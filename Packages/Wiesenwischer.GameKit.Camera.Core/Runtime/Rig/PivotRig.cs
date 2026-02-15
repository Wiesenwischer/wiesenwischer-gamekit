using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Hierarchische Camera-Rig-Struktur mit getrennten Rotationsachsen.
    /// Wird vom CameraBrain pro Frame mit dem finalen CameraState aktualisiert.
    /// </summary>
    public class PivotRig : MonoBehaviour
    {
        [SerializeField] private Transform _yawPivot;
        [SerializeField] private Transform _pitchPivot;
        [SerializeField] private Transform _offsetPivot;
        [SerializeField] private Transform _cameraTransform;

        public Transform CameraTransform => _cameraTransform;
        public Transform Root => transform;

        /// <summary>
        /// Wendet den CameraState auf die Hierarchie an.
        /// Aufgerufen vom CameraBrain nach Behaviour-Evaluation.
        /// </summary>
        public void ApplyState(CameraState state, Vector3 anchorPosition)
        {
            transform.position = anchorPosition;
            _yawPivot.localRotation = Quaternion.Euler(0f, state.Yaw, 0f);
            _pitchPivot.localRotation = Quaternion.Euler(state.Pitch, 0f, 0f);
            _offsetPivot.localPosition = state.ShoulderOffset;
            _cameraTransform.localPosition = new Vector3(0f, 0f, -state.Distance);
        }

        /// <summary>
        /// Erstellt die Pivot-Hierarchie automatisch als Child-Transforms.
        /// </summary>
        public void EnsureHierarchy()
        {
            if (_yawPivot == null)
                _yawPivot = CreateChild(transform, "YawPivot");
            if (_pitchPivot == null)
                _pitchPivot = CreateChild(_yawPivot, "PitchPivot");
            if (_offsetPivot == null)
                _offsetPivot = CreateChild(_pitchPivot, "OffsetPivot");
            if (_cameraTransform == null)
            {
                var cam = GetComponentInChildren<UnityEngine.Camera>();
                if (cam != null)
                    _cameraTransform = cam.transform;
                else
                    _cameraTransform = CreateChild(_offsetPivot, "Camera");
            }
        }

        /// <summary>Welt-Position der Kamera.</summary>
        public Vector3 GetCameraWorldPosition()
            => _cameraTransform.position;

        /// <summary>Welt-Rotation der Kamera.</summary>
        public Quaternion GetCameraWorldRotation()
            => _cameraTransform.rotation;

        /// <summary>Forward-Vektor der Kamera in Welt-Space.</summary>
        public Vector3 GetCameraForward()
            => _cameraTransform.forward;

        private static Transform CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            return go.transform;
        }
    }
}
