using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Input
{
    /// <summary>
    /// Interface für Input Provider.
    /// Abstrahiert die Eingabequelle (Spieler, KI, Netzwerk, Replay).
    /// </summary>
    public interface IMovementInputProvider
    {
        /// <summary>
        /// Die aktuelle Bewegungsrichtung.
        /// X = horizontal (strafe), Y = vertikal (forward/backward).
        /// Werte zwischen -1 und 1.
        /// </summary>
        Vector2 MoveInput { get; }

        /// <summary>
        /// Die aktuelle Blickrichtung (für Kamera/Rotation).
        /// </summary>
        Vector2 LookInput { get; }

        /// <summary>
        /// Ob der Jump-Button diesen Frame gedrückt wurde.
        /// </summary>
        bool JumpPressed { get; }

        /// <summary>
        /// Ob der Jump-Button gehalten wird.
        /// </summary>
        bool JumpHeld { get; }

        /// <summary>
        /// Ob der Sprint-Button gehalten wird.
        /// </summary>
        bool SprintHeld { get; }

        /// <summary>
        /// Ob der Dash-Button diesen Frame gedrückt wurde.
        /// </summary>
        bool DashPressed { get; }

        /// <summary>
        /// Ob der Input Provider aktiv ist (z.B. für Netzwerk-Authority).
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Wird am Anfang jedes Ticks aufgerufen, um den Input zu aktualisieren.
        /// Bei Unity Input System: Input wird hier aus den Actions gelesen.
        /// Bei Netzwerk: Input kommt aus dem Buffer.
        /// </summary>
        void UpdateInput();

        /// <summary>
        /// Setzt den Input zurück (z.B. bei Fokusverlust).
        /// </summary>
        void ResetInput();
    }

    /// <summary>
    /// Struct für serialisierbaren Input (für Netzwerk/Replay).
    /// Enthält alle relevanten Eingaben für einen einzelnen Tick.
    /// </summary>
    [System.Serializable]
    public struct InputSnapshot
    {
        /// <summary>
        /// Der Tick, zu dem dieser Input gehört.
        /// </summary>
        public int Tick;

        /// <summary>
        /// Bewegungsrichtung (-1 bis 1).
        /// </summary>
        public Vector2 MoveInput;

        /// <summary>
        /// Blickrichtung (Yaw/Pitch in Grad).
        /// </summary>
        public Vector2 LookInput;

        /// <summary>
        /// Button-States als Bitflags.
        /// </summary>
        public InputButtons Buttons;

        /// <summary>
        /// Timestamp für Latenz-Berechnung.
        /// </summary>
        public float Timestamp;

        /// <summary>
        /// Prüft ob ein bestimmter Button gedrückt ist.
        /// </summary>
        public bool HasButton(InputButtons button) => (Buttons & button) != 0;

        /// <summary>
        /// Erstellt einen leeren Snapshot.
        /// </summary>
        public static InputSnapshot Empty => new InputSnapshot
        {
            Tick = 0,
            MoveInput = Vector2.zero,
            LookInput = Vector2.zero,
            Buttons = InputButtons.None,
            Timestamp = 0f
        };
    }

    /// <summary>
    /// Bitflags für Button-Inputs.
    /// Kompakt für Netzwerk-Übertragung.
    /// </summary>
    [System.Flags]
    public enum InputButtons : byte
    {
        None = 0,
        Jump = 1 << 0,
        Sprint = 1 << 1,
        Dash = 1 << 2,
        Crouch = 1 << 3,
        Interact = 1 << 4,
        Attack = 1 << 5,
        Block = 1 << 6,
        // Bit 7 reserviert für Erweiterungen
    }
}
