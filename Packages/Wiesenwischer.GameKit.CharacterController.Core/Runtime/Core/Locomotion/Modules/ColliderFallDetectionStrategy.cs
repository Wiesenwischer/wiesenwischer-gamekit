using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;

namespace Wiesenwischer.GameKit.CharacterController.Core.Locomotion.Modules
{
    /// <summary>
    /// Collider-basierte Fall Detection Strategy (Genshin-Style).
    /// Raycast von Capsule-Unterseite nach unten.
    /// Wenn unter der Capsule-Unterseite kein Boden gefunden wird
    /// → Character sollte fallen.
    /// </summary>
    public class ColliderFallDetectionStrategy : IFallDetectionStrategy
    {
        private readonly float _rayDistance;
        private readonly LayerMask _groundLayers;

        public bool IsOverEdge { get; private set; }

        public ColliderFallDetectionStrategy(float rayDistance, LayerMask groundLayers)
        {
            _rayDistance = rayDistance;
            _groundLayers = groundLayers;
        }

        public void Evaluate(CharacterMotor motor)
        {
            Vector3 capsuleBottom = motor.TransientPosition;

            bool groundFound = Physics.Raycast(
                capsuleBottom,
                Vector3.down,
                out _,
                _rayDistance,
                _groundLayers,
                QueryTriggerInteraction.Ignore);

            IsOverEdge = !groundFound;
        }
    }
}
