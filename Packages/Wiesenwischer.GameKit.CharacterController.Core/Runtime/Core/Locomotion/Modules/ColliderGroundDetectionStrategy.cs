using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;

namespace Wiesenwischer.GameKit.CharacterController.Core.Locomotion.Modules
{
    /// <summary>
    /// Collider-basierte Ground Detection Strategy (Genshin-Style).
    /// IsGrounded: SphereCast von Capsule-Unterseite nach unten.
    /// </summary>
    public class ColliderGroundDetectionStrategy : IGroundDetectionStrategy
    {
        private readonly float _checkDistance;
        private readonly float _checkRadius;
        private readonly LayerMask _groundLayers;

        public bool IsGrounded { get; private set; }

        public ColliderGroundDetectionStrategy(float checkDistance, float checkRadius, LayerMask groundLayers)
        {
            _checkDistance = checkDistance;
            _checkRadius = checkRadius;
            _groundLayers = groundLayers;
        }

        public void Evaluate(CharacterMotor motor)
        {
            IsGrounded = CheckGroundByCollider(motor);
        }

        /// <summary>
        /// SphereCast von Capsule-Unterseite nach unten.
        /// </summary>
        private bool CheckGroundByCollider(CharacterMotor motor)
        {
            Vector3 capsuleBottom = motor.TransientPosition + Vector3.up * _checkRadius;
            return Physics.SphereCast(
                capsuleBottom,
                _checkRadius,
                Vector3.down,
                out _,
                _checkDistance + _checkRadius,
                _groundLayers,
                QueryTriggerInteraction.Ignore);
        }
    }
}
