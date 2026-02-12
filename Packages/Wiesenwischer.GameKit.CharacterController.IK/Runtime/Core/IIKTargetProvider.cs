using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.IK
{
    /// <summary>
    /// Liefert IK-Zielpunkte. Verschiedene Quellen möglich:
    /// - Kamera (LookAt-Target)
    /// - Gameplay (Gegner-Position für LookAt)
    /// - Netzwerk (Remote Player Target Sync)
    /// </summary>
    public interface IIKTargetProvider
    {
        /// <summary>
        /// Punkt, den der Character anschauen soll (für LookAt IK).
        /// </summary>
        Vector3 GetLookTarget();

        /// <summary>
        /// Ob aktuell ein gültiges LookAt-Target vorhanden ist.
        /// Wenn false, blendet LookAtIK den Weight auf 0.
        /// </summary>
        bool HasLookTarget { get; }
    }
}
