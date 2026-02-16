using UnityEngine;
#if CINEMACHINE_AVAILABLE
using Cinemachine;
#endif

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Optionale Rendering-Schicht: Überträgt den CameraState auf eine Cinemachine VirtualCamera.
    /// Cinemachine wird nur zum Rendern verwendet — keine Gameplay-Logik.
    /// Funktioniert nur wenn Cinemachine im Projekt installiert ist.
    /// </summary>
    public class CinemachineDriver : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("CameraBrain von dem der State gelesen wird")]
        [SerializeField] private CameraBrain _brain;

        [Tooltip("PivotRig für die finale Transform-Position")]
        [SerializeField] private PivotRig _pivotRig;

#if CINEMACHINE_AVAILABLE
        [Header("Cinemachine")]
        [Tooltip("Cinemachine VirtualCamera die gesteuert wird")]
        [SerializeField] private CinemachineVirtualCamera _virtualCamera;

        [Tooltip("Sync-Modus")]
        [SerializeField] private SyncMode _syncMode = SyncMode.PositionRotationFov;

        public enum SyncMode
        {
            /// <summary>Nur Position und Rotation.</summary>
            PositionRotation,
            /// <summary>Position, Rotation und FOV.</summary>
            PositionRotationFov
        }

        private void LateUpdate()
        {
            if (_virtualCamera == null || _pivotRig == null || _brain == null)
                return;

            var cameraTransform = _virtualCamera.transform;
            cameraTransform.position = _pivotRig.GetCameraWorldPosition();
            cameraTransform.rotation = _pivotRig.GetCameraWorldRotation();

            if (_syncMode == SyncMode.PositionRotationFov)
            {
                _virtualCamera.m_Lens.FieldOfView = _brain.State.Fov;
            }
        }

        private void OnValidate()
        {
            if (_brain == null)
                _brain = GetComponentInParent<CameraBrain>();
            if (_pivotRig == null)
                _pivotRig = GetComponentInParent<PivotRig>();
        }
#else
        private void Awake()
        {
            Debug.LogWarning(
                "[CinemachineDriver] Cinemachine ist nicht installiert. " +
                "Der Driver hat keine Funktion. " +
                "Installiere 'com.unity.cinemachine' über den Package Manager.");
            enabled = false;
        }
#endif
    }
}
