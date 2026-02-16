namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Optionales Interface für ICameraBehaviours die Preset-Werte empfangen können.
    /// CameraBrain.SetPreset() prüft jedes Behaviour auf dieses Interface.
    /// </summary>
    public interface ICameraPresetReceiver
    {
        /// <summary>
        /// Übernimmt die relevanten Werte aus dem Preset.
        /// Jedes Behaviour liest nur die für sich relevanten Felder.
        /// </summary>
        void ApplyPreset(CameraPreset preset);
    }
}
