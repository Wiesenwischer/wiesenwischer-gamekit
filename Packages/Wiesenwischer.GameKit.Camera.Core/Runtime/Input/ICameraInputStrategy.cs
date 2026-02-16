using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Strategie für mode-spezifische Input-Interpretation.
    /// Wird vom CameraPreset bestimmt und in die Pipeline injiziert.
    /// Ersetzt das if/else auf OrbitActivation in DetermineOrbitMode().
    /// </summary>
    public interface ICameraInputStrategy
    {
        /// <summary>Bestimmt OrbitMode basierend auf Button-State.</summary>
        CameraOrbitMode DetermineOrbitMode(bool freeLookHeld, bool steerHeld, bool isGamepad);

        /// <summary>Soll Look-Input gelesen werden?</summary>
        bool ShouldReadLookInput(CameraOrbitMode mode);

        /// <summary>Initialer Cursor-State beim Aktivieren der Strategy.</summary>
        CursorLockMode InitialCursorState { get; }

        /// <summary>Cursor-State basierend auf aktuellem OrbitMode.</summary>
        CursorLockMode GetCursorState(CameraOrbitMode mode);
    }
}
