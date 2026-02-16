using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Bestimmt, wie der Character rotiert/facing bestimmt wird.
    /// Getrennt von IOrientationProvider, da Facing und Frame-Space
    /// unabhängige Konzepte sind.
    /// </summary>
    public interface IFacingProvider
    {
        /// <summary>Aktueller Facing-Modus.</summary>
        FacingMode GetFacingMode();

        /// <summary>
        /// Zielrichtung für CameraForward/TargetLockOn Modi.
        /// Nur relevant wenn FacingMode != MovementDirection.
        /// </summary>
        Vector3 GetFacingDirection();
    }
}
