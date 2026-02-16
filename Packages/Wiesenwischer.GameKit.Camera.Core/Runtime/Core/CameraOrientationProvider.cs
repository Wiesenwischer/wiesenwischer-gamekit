using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Bridge zwischen Camera-System und Character-Controller.
    /// Übersetzt CameraBrain-State in IOrientationProvider/IFacingProvider Semantik.
    /// Sitzt auf dem CameraBrain-GameObject.
    /// </summary>
    [RequireComponent(typeof(CameraBrain))]
    public class CameraOrientationProvider : MonoBehaviour,
        IOrientationProvider, IFacingProvider
    {
        private CameraBrain _brain;

        private void Awake()
        {
            _brain = GetComponent<CameraBrain>();
        }

        // --- IOrientationProvider ---

        public Vector3 GetMovementForward()
        {
            bool useCameraFrame =
                _brain.OrbitActivation == OrbitActivation.AlwaysOn
                || _brain.CurrentOrbitMode == CameraOrbitMode.SteerOrbit;

            if (useCameraFrame)
            {
                // Camera Forward (Y=0, normalisiert)
                return _brain.Forward;
            }
            else
            {
                // Character Forward
                var target = _brain.FollowTarget;
                return target != null ? target.forward : Vector3.forward;
            }
        }

        public Vector3 GetMovementRight()
        {
            Vector3 forward = GetMovementForward();
            return Vector3.Cross(Vector3.up, forward).normalized;
        }

        // --- IFacingProvider ---

        public FacingMode GetFacingMode()
        {
            if (_brain.OrbitActivation == OrbitActivation.AlwaysOn)
                return FacingMode.MovementDirection;

            // ButtonActivated (ClassicMMO)
            return _brain.CurrentOrbitMode switch
            {
                CameraOrbitMode.SteerOrbit => FacingMode.CameraForward,
                _ => FacingMode.MovementDirection
            };
        }

        public Vector3 GetFacingDirection()
        {
            return _brain.Forward;
        }
    }
}
