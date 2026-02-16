namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Bestimmt, was die Character-Rotation antreibt.
    /// </summary>
    public enum FacingMode
    {
        /// <summary>
        /// Character rotiert in Bewegungsrichtung (aktuelles Default-Verhalten).
        /// Funktioniert sowohl im Camera-Frame als auch Character-Frame.
        /// </summary>
        MovementDirection,

        /// <summary>
        /// Character alignt sich zur Kamera-Vorwärtsrichtung (ClassicMMO SteerOrbit).
        /// Character dreht sich auch ohne Bewegung zur Kamera.
        /// </summary>
        CameraForward,

        /// <summary>
        /// Character schaut zum Lock-On Target (Zukunft: Combat).
        /// </summary>
        TargetLockOn,

        /// <summary>
        /// Keine Facing-Änderung. Character behält aktuelle Rotation.
        /// </summary>
        None
    }
}
