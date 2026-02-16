using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Interface für Kamera-Orbit-Information.
    /// Implementiert von CameraBrain (Camera.Core Package).
    /// Gelesen von PlayerController um Frame-Space und Steer-Verhalten zu bestimmen.
    /// </summary>
    public interface ICameraOrbitProvider
    {
        /// <summary>
        /// True wenn der aktuelle Frame im Steer-Modus ist.
        /// SteerOrbit: Character soll sofort zur Kamera-Richtung rotieren.
        /// FreeOrbit/None: Character rotiert nur in Bewegungsrichtung.
        /// </summary>
        bool IsSteerMode { get; }

        /// <summary>
        /// Kamera Forward-Vektor (Y=0, normalisiert).
        /// </summary>
        Vector3 Forward { get; }
    }
}
