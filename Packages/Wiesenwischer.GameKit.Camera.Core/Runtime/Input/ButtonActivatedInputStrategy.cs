using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// ClassicMMO-Mode: LMB = FreeOrbit, RMB = SteerOrbit.
    /// Ohne Button = kein Orbit, Cursor frei.
    /// Gamepad = immer FreeOrbit (Sticks steuern Kamera).
    /// </summary>
    public class ButtonActivatedInputStrategy : ICameraInputStrategy
    {
        public CameraOrbitMode DetermineOrbitMode(bool freeLookHeld, bool steerHeld, bool isGamepad)
        {
            if (isGamepad) return CameraOrbitMode.FreeOrbit;
            if (steerHeld) return CameraOrbitMode.SteerOrbit;
            if (freeLookHeld) return CameraOrbitMode.FreeOrbit;
            return CameraOrbitMode.None;
        }

        public bool ShouldReadLookInput(CameraOrbitMode mode)
            => mode != CameraOrbitMode.None;

        public CursorLockMode InitialCursorState => CursorLockMode.None;

        public CursorLockMode GetCursorState(CameraOrbitMode mode)
            => mode != CameraOrbitMode.None ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
