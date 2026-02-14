using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.Abilities.Core
{
    /// <summary>
    /// Read-only context container passed to abilities on every call.
    /// Provides access to the character's systems without tight coupling.
    /// </summary>
    public readonly struct AbilityContext
    {
        /// <summary>Referenz auf den PlayerController (Orchestrator).</summary>
        public PlayerController Player { get; }

        /// <summary>Referenz auf den CharacterMotor (Positions/Velocity-Daten).</summary>
        public CharacterMotor Motor { get; }

        /// <summary>Referenz auf die CharacterLocomotion (Grounding, Movement).</summary>
        public CharacterLocomotion Locomotion { get; }

        /// <summary>Referenz auf den AnimationController (Layer-Steuerung).</summary>
        public IAnimationController AnimationController { get; }

        /// <summary>Ob der Character aktuell am Boden ist.</summary>
        public bool IsGrounded => Locomotion != null && Locomotion.IsGrounded;

        /// <summary>Aktuelle Geschwindigkeit des Characters.</summary>
        public Vector3 Velocity => Motor != null ? Motor.Velocity : Vector3.zero;

        public AbilityContext(
            PlayerController player,
            CharacterMotor motor,
            CharacterLocomotion locomotion,
            IAnimationController animationController)
        {
            Player = player;
            Motor = motor;
            Locomotion = locomotion;
            AnimationController = animationController;
        }
    }
}
