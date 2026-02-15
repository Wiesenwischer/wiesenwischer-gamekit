using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Globale Konfiguration für das Camera Core System.
    /// Behaviour-spezifische Parameter liegen auf den jeweiligen ICameraBehaviour-Komponenten.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CameraCoreConfig",
        menuName = "Wiesenwischer/GameKit/Camera Core Config",
        order = 2)]
    public class CameraCoreConfig : ScriptableObject
    {
        [Header("Global")]
        [Tooltip("Standard-FOV")]
        public float DefaultFov = 60f;
    }
}
