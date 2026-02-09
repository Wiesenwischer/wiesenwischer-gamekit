using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation
{
    /// <summary>
    /// Konstanten für Animator-Parameter.
    /// Verwendet Hash-IDs für bessere Performance.
    /// </summary>
    public static class AnimationParameters
    {
        // Parameter-Namen
        public const string SpeedParam = "Speed";
        public const string IsGroundedParam = "IsGrounded";
        public const string VerticalVelocityParam = "VerticalVelocity";
        public const string JumpTrigger = "Jump";
        public const string LandTrigger = "Land";
        public const string HardLandingParam = "HardLanding";

        // Hash-IDs (für Performance)
        public static readonly int SpeedHash = Animator.StringToHash(SpeedParam);
        public static readonly int IsGroundedHash = Animator.StringToHash(IsGroundedParam);
        public static readonly int VerticalVelocityHash = Animator.StringToHash(VerticalVelocityParam);
        public static readonly int JumpHash = Animator.StringToHash(JumpTrigger);
        public static readonly int LandHash = Animator.StringToHash(LandTrigger);
        public static readonly int HardLandingHash = Animator.StringToHash(HardLandingParam);

        // Layer-Indizes
        public const int BaseLayerIndex = 0;
        public const int AbilityLayerIndex = 1;
        public const int StatusLayerIndex = 2;
    }
}
