namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Modulares Camera-Verhalten. Modifiziert den CameraState basierend auf dem CameraContext.
    /// Behaviours werden vom CameraBrain in definierter Reihenfolge ausgeführt.
    /// </summary>
    public interface ICameraBehaviour
    {
        /// <summary>
        /// Wird der Behaviour aktuell evaluiert?
        /// CameraBrain überspringt deaktivierte Behaviours.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Modifiziert den CameraState. Wird jeden Frame aufgerufen wenn IsActive = true.
        /// </summary>
        void UpdateState(ref CameraState state, CameraContext ctx);
    }
}
