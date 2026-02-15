namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Gefilterter Camera-Input pro Frame (nach Deadzone, Acceleration, Smoothing).
    /// </summary>
    public struct CameraInputState
    {
        /// <summary>Gefilterte horizontale Look-Rotation (Grad/s).</summary>
        public float LookX;

        /// <summary>Gefilterte vertikale Look-Rotation (Grad/s).</summary>
        public float LookY;

        /// <summary>Zoom-Input (-1 = rein, +1 = raus).</summary>
        public float Zoom;

        /// <summary>True wenn der Input von einem Gamepad kommt.</summary>
        public bool IsGamepad;
    }
}
