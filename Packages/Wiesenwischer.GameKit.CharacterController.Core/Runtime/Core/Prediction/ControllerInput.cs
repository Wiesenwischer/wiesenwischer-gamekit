using System;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Prediction
{
    /// <summary>
    /// Serialisierbare Struktur für Controller-Input.
    /// Wird für Netzwerk-Synchronisation und Prediction verwendet.
    /// </summary>
    [Serializable]
    public struct ControllerInput : IEquatable<ControllerInput>
    {
        /// <summary>
        /// Tick bei dem dieser Input aufgezeichnet wurde.
        /// </summary>
        public int Tick;

        /// <summary>
        /// Bewegungsrichtung (X = horizontal, Y = forward/backward).
        /// Normalisiert oder mit Magnitude <= 1.
        /// </summary>
        public Vector2 MoveDirection;

        /// <summary>
        /// Blickrichtung / Kamera-Rotation.
        /// </summary>
        public Vector2 LookDirection;

        /// <summary>
        /// Rotation des Characters (Y-Achse in Grad).
        /// </summary>
        public float Rotation;

        /// <summary>
        /// Gedrückte Buttons als Bit-Flags.
        /// </summary>
        public ControllerButtons Buttons;

        /// <summary>
        /// Zeitstempel (lokale Zeit).
        /// </summary>
        public float Timestamp;

        /// <summary>
        /// Ob Jump gedrückt ist.
        /// </summary>
        public bool Jump => (Buttons & ControllerButtons.Jump) != 0;

        /// <summary>
        /// Ob Sprint gedrückt ist.
        /// </summary>
        public bool Sprint => (Buttons & ControllerButtons.Sprint) != 0;

        /// <summary>
        /// Ob Crouch gedrückt ist.
        /// </summary>
        public bool Crouch => (Buttons & ControllerButtons.Crouch) != 0;

        /// <summary>
        /// Ob Primary Action (z.B. Attack) gedrückt ist.
        /// </summary>
        public bool PrimaryAction => (Buttons & ControllerButtons.PrimaryAction) != 0;

        /// <summary>
        /// Ob Secondary Action (z.B. Block) gedrückt ist.
        /// </summary>
        public bool SecondaryAction => (Buttons & ControllerButtons.SecondaryAction) != 0;

        /// <summary>
        /// Ob Interact gedrückt ist.
        /// </summary>
        public bool Interact => (Buttons & ControllerButtons.Interact) != 0;

        /// <summary>
        /// Erstellt einen leeren Input für einen gegebenen Tick.
        /// </summary>
        public static ControllerInput Empty(int tick)
        {
            return new ControllerInput
            {
                Tick = tick,
                MoveDirection = Vector2.zero,
                LookDirection = Vector2.zero,
                Rotation = 0f,
                Buttons = ControllerButtons.None,
                Timestamp = 0f
            };
        }

        /// <summary>
        /// Erstellt einen Input mit Bewegungsdaten.
        /// </summary>
        public static ControllerInput Create(int tick, Vector2 move, Vector2 look, float rotation, ControllerButtons buttons)
        {
            return new ControllerInput
            {
                Tick = tick,
                MoveDirection = move,
                LookDirection = look,
                Rotation = rotation,
                Buttons = buttons,
                Timestamp = Time.time
            };
        }

        public bool Equals(ControllerInput other)
        {
            return Tick == other.Tick &&
                   MoveDirection.Equals(other.MoveDirection) &&
                   LookDirection.Equals(other.LookDirection) &&
                   Mathf.Approximately(Rotation, other.Rotation) &&
                   Buttons == other.Buttons;
        }

        public override bool Equals(object obj)
        {
            return obj is ControllerInput other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Tick, MoveDirection, LookDirection, Rotation, Buttons);
        }

        public static bool operator ==(ControllerInput left, ControllerInput right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ControllerInput left, ControllerInput right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"[Tick {Tick}] Move:{MoveDirection} Rot:{Rotation:F1} Buttons:{Buttons}";
        }
    }

    /// <summary>
    /// Bit-Flags für Controller Buttons.
    /// </summary>
    [Flags]
    public enum ControllerButtons : ushort
    {
        None = 0,
        Jump = 1 << 0,
        Sprint = 1 << 1,
        Crouch = 1 << 2,
        PrimaryAction = 1 << 3,
        SecondaryAction = 1 << 4,
        Interact = 1 << 5,
        Ability1 = 1 << 6,
        Ability2 = 1 << 7,
        Ability3 = 1 << 8,
        Ability4 = 1 << 9,
        // Reserve bits 10-15 for future use
    }
}
