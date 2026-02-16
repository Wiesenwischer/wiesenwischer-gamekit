namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Aktueller Orbit-Zustand pro Frame.
    /// Bestimmt ob und wie Kamerarotation angewendet wird.
    /// </summary>
    public enum CameraOrbitMode
    {
        /// <summary>Kein Orbit-Input aktiv. Cursor frei.</summary>
        None,

        /// <summary>Kamera rotiert frei um den Character. Character dreht nicht mit.</summary>
        FreeOrbit,

        /// <summary>Kamera rotiert UND Character soll sich mitdrehen (Steer-Modus).</summary>
        SteerOrbit
    }
}
