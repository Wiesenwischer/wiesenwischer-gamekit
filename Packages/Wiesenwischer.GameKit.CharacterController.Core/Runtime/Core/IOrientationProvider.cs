using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Liefert den Referenzraum für Movement-Input-Interpretation.
    /// Implementierungen: CameraOrientationProvider (Camera-Package),
    /// VehicleOrientationProvider (Mount-Package, Zukunft).
    /// </summary>
    public interface IOrientationProvider
    {
        /// <summary>
        /// Forward-Richtung für Movement-Input.
        /// Camera-Frame: Camera Forward (Y=0, normalisiert).
        /// Character-Frame: Character Forward.
        /// </summary>
        Vector3 GetMovementForward();

        /// <summary>
        /// Right-Richtung für Movement-Input.
        /// </summary>
        Vector3 GetMovementRight();
    }
}
