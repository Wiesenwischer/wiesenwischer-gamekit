using System;

namespace Wiesenwischer.GameKit.CharacterController.Core.Prediction
{
    /// <summary>
    /// Bit-Flags fuer Controller Buttons.
    /// Verwendet in MoveReplicateData fuer Netzwerk-Synchronisation.
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
        Walk = 1 << 10,
        // Reserve bits 11-15 for future use
    }
}
