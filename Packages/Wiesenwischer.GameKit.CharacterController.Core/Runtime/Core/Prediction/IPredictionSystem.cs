using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Input;

namespace Wiesenwischer.GameKit.CharacterController.Core.Prediction
{
    /// <summary>
    /// Interface für das Client-Side Prediction System.
    /// Verwaltet Input-Buffer, State-History und Server-Reconciliation.
    /// </summary>
    public interface IPredictionSystem
    {
        /// <summary>
        /// Der aktuelle Tick-Index (lokal).
        /// </summary>
        int CurrentTick { get; }

        /// <summary>
        /// Der letzte vom Server bestätigte Tick.
        /// </summary>
        int LastAcknowledgedTick { get; }

        /// <summary>
        /// Ob das System aktiv ist (Netzwerk verbunden).
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Speichert den Input für einen bestimmten Tick.
        /// </summary>
        /// <param name="tick">Der Tick-Index.</param>
        /// <param name="input">Der Input für diesen Tick.</param>
        void RecordInput(int tick, InputSnapshot input);

        /// <summary>
        /// Speichert den State für einen bestimmten Tick.
        /// </summary>
        /// <param name="tick">Der Tick-Index.</param>
        /// <param name="state">Der State für diesen Tick.</param>
        void RecordState(int tick, PredictionState state);

        /// <summary>
        /// Holt den Input für einen bestimmten Tick aus dem Buffer.
        /// </summary>
        /// <param name="tick">Der Tick-Index.</param>
        /// <param name="input">Der gefundene Input.</param>
        /// <returns>True wenn der Input gefunden wurde.</returns>
        bool TryGetInput(int tick, out InputSnapshot input);

        /// <summary>
        /// Holt den State für einen bestimmten Tick aus dem Buffer.
        /// </summary>
        /// <param name="tick">Der Tick-Index.</param>
        /// <param name="state">Der gefundene State.</param>
        /// <returns>True wenn der State gefunden wurde.</returns>
        bool TryGetState(int tick, out PredictionState state);

        /// <summary>
        /// Wird vom Server aufgerufen, um einen bestätigten State zu setzen.
        /// Löst bei Abweichung einen Rollback aus.
        /// </summary>
        /// <param name="tick">Der Tick des bestätigten States.</param>
        /// <param name="serverState">Der State vom Server.</param>
        void OnServerStateReceived(int tick, PredictionState serverState);

        /// <summary>
        /// Prüft ob ein Rollback nötig ist und führt ihn aus.
        /// </summary>
        /// <param name="resimulateCallback">Callback für Re-Simulation pro Tick.</param>
        void Reconcile(System.Action<int, InputSnapshot> resimulateCallback);

        /// <summary>
        /// Räumt alte Einträge aus den Buffern auf.
        /// </summary>
        /// <param name="oldestTickToKeep">Der älteste Tick, der behalten werden soll.</param>
        void Cleanup(int oldestTickToKeep);
    }

    /// <summary>
    /// Struct für den Prediction State.
    /// Enthält alle Daten, die für Rollback/Reconciliation relevant sind.
    /// </summary>
    [System.Serializable]
    public struct PredictionState
    {
        /// <summary>
        /// Der Tick, zu dem dieser State gehört.
        /// </summary>
        public int Tick;

        /// <summary>
        /// Die Position des Characters.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Die Rotation des Characters (als Quaternion für Präzision).
        /// </summary>
        public Quaternion Rotation;

        /// <summary>
        /// Die aktuelle Geschwindigkeit.
        /// </summary>
        public Vector3 Velocity;

        /// <summary>
        /// Der Name des aktuellen States (z.B. "Grounded", "Airborne").
        /// </summary>
        public string StateName;

        /// <summary>
        /// Ob der Character geerdet ist.
        /// </summary>
        public bool IsGrounded;

        /// <summary>
        /// Vergleicht zwei States auf Gleichheit (mit Toleranz).
        /// </summary>
        public bool Equals(PredictionState other, float positionTolerance = 0.01f)
        {
            return Vector3.Distance(Position, other.Position) <= positionTolerance
                   && Quaternion.Angle(Rotation, other.Rotation) <= 1f // 1 Grad Toleranz
                   && StateName == other.StateName
                   && IsGrounded == other.IsGrounded;
        }

        /// <summary>
        /// Erstellt einen leeren State.
        /// </summary>
        public static PredictionState Empty => new PredictionState
        {
            Tick = 0,
            Position = Vector3.zero,
            Rotation = Quaternion.identity,
            Velocity = Vector3.zero,
            StateName = "",
            IsGrounded = false
        };
    }

    /// <summary>
    /// Interface für Objekte, die Prediction-fähig sind.
    /// </summary>
    public interface IPredictable
    {
        /// <summary>
        /// Erstellt einen Snapshot des aktuellen States.
        /// </summary>
        PredictionState CreateStateSnapshot(int tick);

        /// <summary>
        /// Wendet einen State an (für Rollback).
        /// </summary>
        void ApplyState(PredictionState state);

        /// <summary>
        /// Simuliert einen einzelnen Tick mit dem gegebenen Input.
        /// </summary>
        void SimulateTick(int tick, InputSnapshot input, float deltaTime);
    }
}
