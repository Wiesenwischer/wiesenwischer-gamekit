using FishNet.Object.Prediction;
using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Server-autoritativer State fuer FishNet [Reconcile].
    /// Der Client nutzt diese Daten um seinen lokalen State zu korrigieren.
    /// </summary>
    public struct CharacterReconcileData : IReconcileData
    {
        /// <summary>Autoritative Position des Characters.</summary>
        public Vector3 Position;

        /// <summary>Autoritative Y-Rotation in Grad.</summary>
        public float Rotation;

        /// <summary>Aktuelle Velocity (XZ-Ebene + vertikale Komponente).</summary>
        public Vector3 Velocity;

        /// <summary>Vertikale Velocity (separiert fuer Gravity/Jump).</summary>
        public float VerticalVelocity;

        /// <summary>Ist der Character am Boden?</summary>
        public bool IsGrounded;

        /// <summary>Ist der Character in Crouch-Haltung?</summary>
        public bool IsCrouching;

        /// <summary>Walk-Toggle aktiv?</summary>
        public bool ShouldWalk;

        /// <summary>
        /// Index des aktuellen Movement-States in der StateMachine.
        /// Erlaubt State-Sync ohne den State-Namen zu serialisieren.
        /// </summary>
        public byte MovementStateIndex;

        // --- IReconcileData Implementation ---
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }
}
