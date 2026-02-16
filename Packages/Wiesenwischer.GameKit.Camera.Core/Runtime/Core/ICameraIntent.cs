namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Transiente Kamera-Beeinflussung durch externe Systeme.
    /// Intents werden vom CameraBrain nach Priorität sortiert und auf den CameraState angewendet.
    /// Höhere Priorität = wird zuletzt angewendet (überschreibt).
    /// </summary>
    public interface ICameraIntent
    {
        /// <summary>
        /// Priorität des Intents. Höhere Werte überschreiben niedrigere.
        /// Empfohlene Bereiche:
        ///   0-99: Ambient (Movement Forward Bias, Idle Look-Around)
        ///  100-199: Gameplay (Soft Targeting, Conversation)
        ///  200-299: Combat (Framing, Lock-On)
        ///  300-399: Override (Cutscene, Forced Camera)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Ist der Intent aktuell aktiv? Inaktive Intents werden übersprungen.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Wendet den Intent auf den aktuellen CameraState an.
        /// Kann einzelne Felder modifizieren (z.B. nur Yaw) oder den ganzen State.
        /// </summary>
        void Apply(ref CameraState state, CameraContext ctx);
    }
}
