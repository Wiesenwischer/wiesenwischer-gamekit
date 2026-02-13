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
        public const string FallingTimeParam = "FallingTime";
        public const string IsFallingLongParam = "IsFallingLong";
        public const string JumpTrigger = "Jump";
        public const string LandTrigger = "Land";
        public const string HardLandingParam = "HardLanding";

        // Hash-IDs (für Performance)
        public static readonly int SpeedHash = Animator.StringToHash(SpeedParam);
        public static readonly int IsGroundedHash = Animator.StringToHash(IsGroundedParam);
        public static readonly int VerticalVelocityHash = Animator.StringToHash(VerticalVelocityParam);
        public static readonly int FallingTimeHash = Animator.StringToHash(FallingTimeParam);
        public static readonly int IsFallingLongHash = Animator.StringToHash(IsFallingLongParam);
        public static readonly int JumpHash = Animator.StringToHash(JumpTrigger);
        public static readonly int LandHash = Animator.StringToHash(LandTrigger);
        public static readonly int HardLandingHash = Animator.StringToHash(HardLandingParam);

        // Animator State-Hashes (für CrossFade)
        public static readonly int LocomotionStateHash = Animator.StringToHash("Locomotion");
        public static readonly int JumpStateHash = Animator.StringToHash("Jump");
        public static readonly int FallStateHash = Animator.StringToHash("Fall");
        public static readonly int SoftLandStateHash = Animator.StringToHash("SoftLand");
        public static readonly int HardLandStateHash = Animator.StringToHash("HardLand");
        public static readonly int RollStateHash = Animator.StringToHash("Roll");
        public static readonly int LightStopStateHash = Animator.StringToHash("LightStop");
        public static readonly int MediumStopStateHash = Animator.StringToHash("MediumStop");
        public static readonly int HardStopStateHash = Animator.StringToHash("HardStop");

        // Layer-Indizes
        public const int BaseLayerIndex = 0;
        public const int AbilityLayerIndex = 1;
        public const int StatusLayerIndex = 2;
    }
}
