using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Runtime-Kontext pro Frame. Transportiert Daten zwischen CameraBrain und Behaviours.
    /// </summary>
    public class CameraContext
    {
        /// <summary>Transform dem die Kamera folgt (Character Root).</summary>
        public Transform FollowTarget;

        /// <summary>Optionales LookAt-Ziel (z.B. für Lock-On).</summary>
        public Transform LookTarget;

        /// <summary>Gefilterter Input nach der Pipeline.</summary>
        public CameraInputState Input;

        /// <summary>Stabilisierte Follow-Position (von CameraAnchor).</summary>
        public Vector3 AnchorPosition;

        /// <summary>Frame DeltaTime.</summary>
        public float DeltaTime;

        /// <summary>
        /// Welt-Geschwindigkeit des Follow-Targets (Character).
        /// Wird vom CameraBrain aus dem CharacterController gelesen.
        /// Benötigt für DynamicOrbitCenter und Soft Targeting.
        /// </summary>
        public Vector3 CharacterVelocity;

        /// <summary>
        /// Forward-Richtung des Follow-Targets (Character).
        /// Wird vom CameraBrain aus FollowTarget.forward gelesen.
        /// Benötigt für RecenterBehaviour und DynamicOrbitCenter.
        /// </summary>
        public Vector3 CharacterForward;

        /// <summary>
        /// True wenn der aktuelle Frame im Steer-Modus ist (RMB in Tab-Target-Kameras).
        /// Character Controller kann diesen Wert lesen und sich zur Kamerarichtung ausrichten.
        /// </summary>
        public bool IsSteerMode;
    }
}
