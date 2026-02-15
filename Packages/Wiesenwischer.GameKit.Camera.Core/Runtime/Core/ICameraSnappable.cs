using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Optionales Interface: Behaviour kann bei Teleport/Snap zurückgesetzt werden.
    /// Wird von CameraBrain in SnapBehindTarget aufgerufen.
    /// </summary>
    public interface ICameraSnappable
    {
        void Snap(Vector3 position);
    }
}
