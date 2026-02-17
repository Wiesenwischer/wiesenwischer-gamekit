using FishNet.Object.Prediction;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Prediction;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Input-Daten fuer FishNet [Replicate].
    /// Enthaelt alle Informationen die der Server braucht um einen Tick zu simulieren.
    /// </summary>
    public struct MoveReplicateData : IReplicateData
    {
        /// <summary>Bewegungsrichtung (WASD normalisiert, Screen-Space).</summary>
        public Vector2 MoveDirection;

        /// <summary>Kamera-Yaw in Grad. KRITISCH: Behebt den Yaw=0 Bug aus Phase 6.</summary>
        public float CameraYaw;

        /// <summary>Aktuelle Character-Rotation in Grad.</summary>
        public float CharacterRotation;

        /// <summary>Bitflags fuer Aktionen (Jump, Sprint, Crouch, Walk, etc.).</summary>
        public ControllerButtons Buttons;

        /// <summary>Speed-Modifier (Walk/Run/Sprint Multiplikator).</summary>
        public float SpeedModifier;

        // --- One-Shot Events (zwischen Ticks akkumuliert) ---

        /// <summary>Jump wurde zwischen den Ticks angefordert.</summary>
        public bool JumpRequested;

        /// <summary>Jump-Button losgelassen (Variable Jump Height).</summary>
        public bool JumpCutRequested;

        /// <summary>Vertikale Velocity zuruecksetzen (z.B. bei Landing).</summary>
        public bool ResetVerticalRequested;

        // --- IReplicateData Implementation ---
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }
}
