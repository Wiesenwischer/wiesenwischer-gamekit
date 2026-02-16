using UnityEngine;
using Wiesenwischer.GameKit.Camera;

namespace Wiesenwischer.GameKit.Camera.Presets
{
    /// <summary>
    /// Zentrale Sammlung von Camera-Presets.
    /// Erlaubt Runtime-Zugriff auf vordefinierte und custom Presets.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CameraPresetLibrary",
        menuName = "Wiesenwischer/GameKit/Camera Preset Library",
        order = 2)]
    public class CameraPresetLibrary : ScriptableObject
    {
        [Tooltip("Standard-Preset das beim Start geladen wird")]
        [SerializeField] private CameraPreset _defaultPreset;

        [Tooltip("Alle verfügbaren Presets")]
        [SerializeField] private CameraPreset[] _presets;

        /// <summary>Das Standard-Preset.</summary>
        public CameraPreset DefaultPreset => _defaultPreset;

        /// <summary>Alle verfügbaren Presets.</summary>
        public CameraPreset[] Presets => _presets;

        /// <summary>Findet ein Preset nach Name.</summary>
        public CameraPreset FindByName(string presetName)
        {
            if (_presets == null) return null;
            foreach (var preset in _presets)
            {
                if (preset != null && preset.name == presetName)
                    return preset;
            }
            return null;
        }
    }
}
