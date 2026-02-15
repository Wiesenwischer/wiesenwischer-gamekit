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
            // NaN-Guard: ungültige Werte nicht auf Transforms anwenden
            if (float.IsNaN(state.Yaw) || float.IsNaN(state.Pitch) || float.IsNaN(state.Distance))
                return;

            transform.position = anchorPosition;
            transform.rotation = Quaternion.identity;
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
            // Validierung: _cameraTransform muss Kind von _offsetPivot sein
            if (_cameraTransform != null && !_cameraTransform.IsChildOf(_offsetPivot))
                _cameraTransform = null;

            if (_cameraTransform == null)
            {
                // Check for camera already under offset pivot (correct setup)
                var cam = _offsetPivot.GetComponentInChildren<UnityEngine.Camera>();
                if (cam != null)
                {
                    _cameraTransform = cam.transform;
                }
                else
                {
                    // Create camera child under offset pivot
                    var camGO = CreateChild(_offsetPivot, "Camera");

                    // Migrate root camera if it exists (common: Camera on Main Camera root)
                    var rootCam = GetComponent<UnityEngine.Camera>();
                    if (rootCam != null)
                    {
                        var newCam = camGO.gameObject.AddComponent<UnityEngine.Camera>();
                        newCam.CopyFrom(rootCam);
                        rootCam.enabled = false;

                        // Migrate AudioListener
                        var rootListener = GetComponent<AudioListener>();
                        if (rootListener != null)
                        {
                            camGO.gameObject.AddComponent<AudioListener>();
                            rootListener.enabled = false;
                        }

                        // Ensure Camera.main still works
                        if (gameObject.CompareTag("MainCamera"))
                            camGO.gameObject.tag = "MainCamera";
                    }

                    _cameraTransform = camGO;
                }
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
