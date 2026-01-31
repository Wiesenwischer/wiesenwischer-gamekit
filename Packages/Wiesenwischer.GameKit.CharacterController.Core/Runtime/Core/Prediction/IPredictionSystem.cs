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

    // PredictionState is defined in PredictionBuffer.cs

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
