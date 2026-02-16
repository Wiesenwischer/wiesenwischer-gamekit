using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Konvertiert World-Move-Direction in Character-lokale Richtung
    /// für Animator-Parameter (MoveX/MoveZ).
    /// Statische Utility-Klasse — zustandslos, rein mathematisch.
    /// </summary>
    public static class AnimationSpaceConverter
    {
        /// <summary>
        /// Konvertiert eine Welt-Bewegungsrichtung in character-lokale Koordinaten.
        /// Ergebnis: x = Strafe (positiv = rechts), z = Forward/Backward (positiv = vorwärts).
        /// </summary>
        /// <param name="worldMoveDir">Normalisierte Bewegungsrichtung in Weltkoordinaten.</param>
        /// <param name="characterTransform">Transform des Characters für lokale Konvertierung.</param>
        /// <returns>Lokale Richtung: x = strafe, z = forward.</returns>
        public static Vector3 WorldToLocal(Vector3 worldMoveDir, Transform characterTransform)
        {
            if (worldMoveDir.sqrMagnitude < 0.001f)
                return Vector3.zero;

            return characterTransform.InverseTransformDirection(worldMoveDir);
        }

        /// <summary>
        /// Berechnet den signierten Winkel zwischen Character-Forward und Bewegungsrichtung.
        /// Positiv = Drehung nach rechts, Negativ = nach links.
        /// Nützlich für Turn-Animationen.
        /// </summary>
        /// <param name="worldMoveDir">Normalisierte Bewegungsrichtung in Weltkoordinaten.</param>
        /// <param name="characterForward">Forward-Richtung des Characters.</param>
        /// <returns>Signierter Winkel in Grad (-180 bis 180).</returns>
        public static float GetTurnAngle(Vector3 worldMoveDir, Vector3 characterForward)
        {
            if (worldMoveDir.sqrMagnitude < 0.001f)
                return 0f;

            Vector3 flatMove = new Vector3(worldMoveDir.x, 0, worldMoveDir.z).normalized;
            Vector3 flatForward = new Vector3(characterForward.x, 0, characterForward.z).normalized;

            return Vector3.SignedAngle(flatForward, flatMove, Vector3.up);
        }
    }
}
