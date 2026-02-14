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
    }
}
