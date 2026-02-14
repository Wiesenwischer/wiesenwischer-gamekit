using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.IK
{
    /// <summary>
    /// Interface für ein IK-Modul. Module werden vom IKManager orchestriert
    /// und im OnAnimatorIK-Callback aufgerufen.
    /// </summary>
    public interface IIKModule
    {
        /// <summary>
        /// Anzeigename für Debug/Editor.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Ob das Modul aktiv ist. Inaktive Module werden nicht aufgerufen.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Globaler Weight des Moduls (0 = kein IK, 1 = voller IK-Einfluss).
        /// Wird vom IKManager an die Unity IK API weitergegeben.
        /// </summary>
        float Weight { get; set; }

        /// <summary>
        /// Wird vom IKManager in OnAnimatorIK aufgerufen.
        /// Das Modul soll hier Animator.SetIKPosition/Rotation/LookAt aufrufen.
        /// </summary>
        /// <param name="animator">Der Animator mit IK-Pass</param>
        /// <param name="layerIndex">Aktueller Animator Layer (meist 0)</param>
        void ProcessIK(Animator animator, int layerIndex);

        /// <summary>
        /// Wird in LateUpdate aufgerufen, um IK-Ziele vorzubereiten
        /// (z.B. Raycasts, Target-Berechnung). Trennung von Vorbereitung
        /// und Anwendung, da OnAnimatorIK zu einem anderen Zeitpunkt läuft.
        /// </summary>
        void PrepareIK();
    }
}
