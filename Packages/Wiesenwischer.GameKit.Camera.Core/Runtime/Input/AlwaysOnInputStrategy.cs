using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// ActionCombat-Mode: Maus steuert immer die Kamera.
    /// Cursor ist immer gelockt, immer FreeOrbit.
    /// </summary>
    public class AlwaysOnInputStrategy : ICameraInputStrategy
    {
        public CameraOrbitMode DetermineOrbitMode(bool freeLookHeld, bool steerHeld, bool isGamepad)
            => CameraOrbitMode.FreeOrbit;

        public bool ShouldReadLookInput(CameraOrbitMode mode) => true;

        public CursorLockMode InitialCursorState => CursorLockMode.Locked;

        public CursorLockMode GetCursorState(CameraOrbitMode mode) => CursorLockMode.Locked;
    }
}
