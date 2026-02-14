using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Deklarativer Kamerazustand pro Frame.
    /// Behaviours modifizieren diesen State nacheinander.
    /// </summary>
    public struct CameraState
    {
        /// <summary>Horizontale Rotation in Grad.</summary>
        public float Yaw;

        /// <summary>Vertikale Rotation in Grad (negativ = nach unten).</summary>
        public float Pitch;

        /// <summary>Abstand der Kamera zum Pivot-Punkt.</summary>
        public float Distance;

        /// <summary>Seitlicher Versatz (Over-Shoulder).</summary>
        public Vector3 ShoulderOffset;

        /// <summary>Field of View in Grad.</summary>
        public float Fov;

        /// <summary>Factory-Methode für Standard-State.</summary>
        public static CameraState Default => new CameraState
        {
            Yaw = 0f,
            Pitch = 0f,
            Distance = 5f,
            ShoulderOffset = Vector3.zero,
            Fov = 60f
        };
    }
}
