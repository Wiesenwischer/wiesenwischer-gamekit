namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Optionales Interface: Behaviour kann den CameraState bei der Initialisierung setzen.
    /// Wird von CameraBrain in Awake und SnapBehindTarget aufgerufen.
    /// </summary>
    public interface ICameraStateInitializer
    {
        void InitializeState(ref CameraState state);
    }
}
