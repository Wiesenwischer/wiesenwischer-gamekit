using System.Collections.Generic;
using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Einheitliches Visual-Smoothing fuer Netzwerk-Characters.
    /// Handhabt BEIDES: Tick-Interpolation UND Reconcile-Correction.
    ///
    /// Verwendet Goal-Queue-basierte Interpolation:
    /// - Jeder Tick pusht die Motor-Position in eine Queue
    /// - LateUpdate konsumiert Goals mit konstanter Rate (1 pro tickDelta)
    /// - Visual interpoliert per Lerp zwischen aufeinanderfolgenden Goals
    /// - Kein Velocity-Extrapolation → kein Overshoot, kein Drift-Akkumulation
    ///
    /// Warum nicht Velocity-Based (vorheriger Ansatz):
    ///   _smoothPos += velocity * dt extrapoliert voraus. Bei unregelmaessigem Tick-Timing
    ///   (ParrelSync: 0-Tick + Multi-Tick Frames) akkumuliert Drift → riesige Offset-Spikes
    ///   (1.5m+) → sichtbarer Stutter mit 6-13:1 Ratio.
    ///   Root Cause: Wenn keine Ticks feuern, rast _smoothPos per velocity*dt voraus.
    ///   Wenn dann Ticks kommen, wird der komplette Drift in einem Frame absorbiert.
    ///
    /// Warum nicht Target-Tracking (MoveTowards, Exponential Smoothing):
    ///   Target springt auf Tick-Frames diskret → variable visuelle Geschwindigkeit.
    ///   Da OnPostTick und LateUpdate im SELBEN Unity-Frame laufen: 5-10:1 Stutter.
    ///
    /// Goal-Queue vermeidet beides:
    ///   - Goals werden mit konstanter Rate konsumiert → gleichmaessige visuelle Geschwindigkeit
    ///   - Multi-Tick-Frames: Queue puffert, LateUpdate konsumiert normal
    ///   - Tick-Luecken: Visual haelt bei letztem Goal, kein Overshoot
    ///   - Drift ist strukturell unmoeglich (kein Extrapolation)
    ///
    /// Architektur:
    ///   _goalQueue      = Motor-Positionen pro Tick (FIFO)
    ///   _fromPos/_toPos = aktuelles Interpolations-Segment
    ///   _interpT        = Fortschritt im Segment (0=from, 1=to)
    ///   _positionOffset = Reconcile-Correction Offset (exponentieller Decay)
    ///
    /// Visual = Lerp(_fromPos, _toPos, _interpT) + _positionOffset
    ///
    /// Ersetzt die KCC-eigene CustomInterpolationUpdate (CharacterMotorSystem.Settings.Interpolate = false).
    /// Damit gibt es nur EIN System das Transform.position in LateUpdate schreibt — kein Kaempfen.
    ///
    /// Flow:
    /// 1. OnPreTick(): Initialisierung (einmalig beim ersten Tick)
    /// 2. [Replicate]: Simulation laeuft, TransientPosition aendert sich
    /// 3. OnReconcileComplete(): Bei Reconcile — Error zum Offset, Queue/Endpoints shiften
    /// 4. OnPostTick(): Goal in Queue pushen
    /// 5. LateUpdate (Order 50): Queue konsumieren, Lerp, Offset-Decay
    ///    → Laeuft VOR CameraBrain (100) und GroundingSmoother (100)
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class ReconcileSmoother : MonoBehaviour
    {
        [Header("Position")]
        [Tooltip("Ab dieser Distanz wird hart geSnapt statt smooth korrigiert.")]
        [SerializeField] private float _snapThreshold = 2f;

        [Tooltip("Decay-Rate pro Frame bei 60fps. 0.25 = ~150ms bis 90% korrigiert.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _correctionRate = 0.25f;

        [Header("Rotation")]
        [Tooltip("Decay-Rate fuer Rotation (Y-Achse).")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _rotationCorrectionRate = 0.25f;

        [Header("Rotation Interpolation")]
        [Tooltip("Zeitkonstante fuer Exponential Smoothing der Rotation.\n" +
                 "Hoeher = smoother aber mehr visueller Lag.\n" +
                 "Empfohlen: 1-2x tickDelta (0.017-0.033 bei 60Hz).")]
        [Range(0.01f, 0.1f)]
        [SerializeField] private float _rotSmoothTime = 0.033f;

        [Header("Thresholds")]
        [Tooltip("Unter diesem Wert wird der Offset auf Zero gesetzt (verhindert Micro-Jitter).")]
        [SerializeField] private float _minCorrectionThreshold = 0.001f;

        [Header("Debug")]
        [Tooltip("Loggt Corrections die groesser als MinCorrectionThreshold sind.")]
        [SerializeField] private bool _debugLog;

        // --- Goal Queue ---
        private const int MaxQueueSize = 4;
        private readonly Queue<Vector3> _goalQueue = new Queue<Vector3>();
        private Vector3 _fromPos;        // Interpolation start (previous consumed goal)
        private Vector3 _toPos;          // Interpolation end (current goal)
        private float _interpT;          // Progress: 0=from, 1=to

        // --- Rotation (exponential smoothing toward target) ---
        private Quaternion _targetRot;
        private Quaternion _smoothRot;

        // --- Correction Offset (Reconcile + Spectator) ---
        private Vector3 _positionOffset;
        private float _rotationOffset;

        // --- State ---
        private bool _initialized;
        private float _tickDelta;

        // --- Diagnostics ---
        private int _diagFrameCount;
        private Vector3 _diagLastFinalPos;
        private Vector3 _diagLastDelta;
        private bool _diagStartupLogged;

        /// <summary>Snap-Threshold fuer externe Abfrage.</summary>
        public float SnapThreshold => _snapThreshold;

        /// <summary>Aktueller visueller Offset (fuer Debug).</summary>
        public Vector3 CurrentOffset => _positionOffset;

        /// <summary>Aktueller Rotations-Offset in Grad (fuer Debug).</summary>
        public float CurrentRotationOffset => _rotationOffset;

        /// <summary>Ob der Smoother aktiv interpoliert.</summary>
        public bool IsActive => _initialized;

        /// <summary>Anzahl gepufferter Goals (fuer Debug/Tests).</summary>
        public int QueueCount => _goalQueue.Count;

        /// <summary>Aktueller Interpolations-Fortschritt (fuer Tests).</summary>
        public float InterpolationT => _interpT;

        #region Tick Lifecycle

        /// <summary>
        /// Wird von NetworkCharacterDriver VOR dem Tick aufgerufen.
        /// Initialisiert den Smoother beim ersten Tick.
        /// </summary>
        public void OnPreTick(Vector3 motorPos, Quaternion motorRot, float tickDelta)
        {
            _tickDelta = tickDelta;

            if (!_initialized)
            {
                _fromPos = _toPos = motorPos;
                _targetRot = _smoothRot = motorRot;
                _interpT = 1f; // Vollstaendig bei _toPos — bereit fuer erstes Goal
                _initialized = true;

                if (_debugLog)
                    Debug.Log($"[Smoother] Initialized at {motorPos:F3}, tickDelta={tickDelta:F4}s");
            }
        }

        /// <summary>
        /// Wird von NetworkCharacterDriver NACH dem Tick aufgerufen.
        /// Pusht die Motor-Position als Goal in die Queue.
        ///
        /// Die Queue puffert Goals und LateUpdate konsumiert sie mit konstanter Rate.
        /// Bei Multi-Tick-Frames werden mehrere Goals gepuffert → smooth abgearbeitet.
        /// Bei Tick-Luecken ist die Queue leer → Visual haelt bei letztem Goal.
        /// </summary>
        public void OnPostTick(Vector3 motorPos, Quaternion motorRot, float tickDelta)
        {
            _tickDelta = tickDelta;
            _targetRot = motorRot;

            if (_initialized)
            {
                _goalQueue.Enqueue(motorPos);

                // Transform auf aktuelle visuelle Position setzen.
                // Goal ist noch nicht konsumiert — Visual bleibt am aktuellen Interpolationspunkt.
                // Wichtig fuer Systeme die zwischen OnPostTick und LateUpdate Transform lesen.
                float t = Mathf.Clamp01(_interpT);
                Vector3 interpPos = Vector3.Lerp(_fromPos, _toPos, t);
                transform.SetPositionAndRotation(
                    interpPos + _positionOffset,
                    _smoothRot * Quaternion.Euler(0f, _rotationOffset, 0f));
            }

            if (_debugLog)
            {
                Debug.Log($"[Smoother] OnPostTick: goal={motorPos:F3} queueSize={_goalQueue.Count} " +
                    $"offset={_positionOffset:F4} |offset|={_positionOffset.magnitude:F4}m");
            }
        }

        #endregion

        #region Reconcile Correction

        /// <summary>
        /// Wird nach Owner-Reconcile+Replay aufgerufen (in PerformReplicate, ContainsTicked).
        /// Berechnet Error, akkumuliert Offset, und shiftet Queue + Endpoints.
        ///
        /// Visual-Stabilitaet:
        ///   Alle Interpolations-Punkte (from, to, queue) werden um die Korrektur verschoben.
        ///   Offset absorbiert den Fehler (Gegenrichtung).
        ///   → Visual = Lerp(shifted_from, shifted_to, t) + (offset + error) = unveraendert.
        ///   → Beim Decay des Offsets gleitet das Visual zur korrigierten Trajektorie.
        /// </summary>
        public void OnReconcileComplete(Vector3 preReconcilePos, float preReconcileRotY,
                                         Vector3 correctedPos, float correctedRotY)
        {
            Vector3 posError = preReconcilePos - correctedPos;
            float rotError = Mathf.DeltaAngle(correctedRotY, preReconcileRotY);

            if (posError.sqrMagnitude > _snapThreshold * _snapThreshold)
            {
                // Hard snap: Visual auf korrigierte Position setzen
                _fromPos = _toPos = correctedPos;
                _goalQueue.Clear();
                _interpT = 1f;
                Quaternion corrRot = Quaternion.Euler(0f, correctedRotY, 0f);
                _targetRot = _smoothRot = corrRot;
                ClearOffset();
            }
            else
            {
                // Smooth correction: Endpoints + Queue um Korrektur shiften, Error zum Offset.
                Vector3 correction = correctedPos - preReconcilePos;
                _fromPos += correction;
                _toPos += correction;

                // Queue-Eintraege shiften (Dequeue + shifted Enqueue)
                int count = _goalQueue.Count;
                for (int i = 0; i < count; i++)
                    _goalQueue.Enqueue(_goalQueue.Dequeue() + correction);

                // Rotation: _smoothRot um Korrektur drehen
                Quaternion rotCorrection = Quaternion.Euler(0f, -rotError, 0f);
                _smoothRot = rotCorrection * _smoothRot;

                _positionOffset += posError;
                _rotationOffset += rotError;
            }

            if (_debugLog && posError.sqrMagnitude > _minCorrectionThreshold * _minCorrectionThreshold)
                Debug.Log($"[ReconcileSmoother] Reconcile: pos={posError.magnitude:F4}m rot={rotError:F2}°");
        }

        /// <summary>
        /// Wird nach Spectator-Correction aufgerufen (nach Simulation mit neuem autoritativem Input).
        /// Gleiche Logik wie OnReconcileComplete: Endpoints + Queue shiften, Error zum Offset.
        /// </summary>
        public void OnSpectatorCorrection(Vector3 prePos, Vector3 postPos, Quaternion postRot)
        {
            Vector3 error = prePos - postPos;

            if (error.sqrMagnitude > _snapThreshold * _snapThreshold)
            {
                _fromPos = _toPos = postPos;
                _goalQueue.Clear();
                _interpT = 1f;
                _targetRot = _smoothRot = postRot;
                ClearOffset();
            }
            else
            {
                Vector3 correction = postPos - prePos;
                _fromPos += correction;
                _toPos += correction;

                int count = _goalQueue.Count;
                for (int i = 0; i < count; i++)
                    _goalQueue.Enqueue(_goalQueue.Dequeue() + correction);

                _positionOffset += error;
            }

            if (_debugLog && error.sqrMagnitude > _minCorrectionThreshold * _minCorrectionThreshold)
                Debug.Log($"[ReconcileSmoother] Spectator: pos={error.magnitude:F4}m");
        }

        /// <summary>Setzt den Offset sofort auf Zero (hard snap).</summary>
        public void ClearOffset()
        {
            _positionOffset = Vector3.zero;
            _rotationOffset = 0f;
        }

        /// <summary>Setzt den Smoother komplett zurueck (z.B. bei Disconnect).</summary>
        public void Reset()
        {
            ClearOffset();
            _goalQueue.Clear();
            _fromPos = _toPos = Vector3.zero;
            _interpT = 0f;
            _initialized = false;
        }

        #endregion

        #region Visual Update

        private void LateUpdate()
        {
            // Offline-Guard: Ohne Initialisierung kein Smoothing.
            // (KCC handhabt Interpolation selbst via CustomInterpolationUpdate)
            if (!_initialized) return;

            // Startup-Log (einmalig)
            if (_debugLog && !_diagStartupLogged)
            {
                _diagStartupLogged = true;
                Debug.Log($"[Smoother] ACTIVE on {gameObject.name} | " +
                    $"snapThreshold={_snapThreshold}m " +
                    $"correctionRate={_correctionRate} " +
                    $"mode=GoalQueue");
            }

            _diagFrameCount++;
            float dt = Time.deltaTime;

            // 1. Goal-Queue Interpolation:
            //    _interpT schreitet mit 1/tickDelta pro Sekunde voran.
            //    Bei _interpT >= 1 und vorhandenen Goals: naechstes Goal konsumieren.
            //    Bei leerer Queue und _interpT >= 1: halten (kein Overshoot).
            bool canAdvance = _interpT < 1f || _goalQueue.Count > 0;
            if (canAdvance && _tickDelta > 0f)
                _interpT += dt / _tickDelta;

            // Goals konsumieren wenn Tick-Grenze ueberschritten
            while (_interpT >= 1f && _goalQueue.Count > 0)
            {
                _fromPos = _toPos;
                _toPos = _goalQueue.Dequeue();
                _interpT -= 1f;
            }

            // Clampen: wenn Queue leer und am Ende, nicht ueber 1 hinaus
            _interpT = Mathf.Min(_interpT, 1f);

            // Safety: Queue-Ueberlauf verhindern (z.B. nach langer Pause mit Tick-Burst)
            while (_goalQueue.Count > MaxQueueSize)
            {
                _fromPos = _toPos;
                _toPos = _goalQueue.Dequeue();
            }

            float t = _interpT;
            Vector3 interpPos = Vector3.Lerp(_fromPos, _toPos, t);

            // 2. Rotation: Exponential Smoothing (Stutter bei Rotation weniger sichtbar)
            float rotAlpha = 1f - Mathf.Exp(-dt / _rotSmoothTime);
            _smoothRot = Quaternion.Slerp(_smoothRot, _targetRot, rotAlpha);

            // 3. Offset-Decay (Reconcile Correction + Spectator Correction)
            bool hasPosition = _positionOffset.sqrMagnitude > _minCorrectionThreshold * _minCorrectionThreshold;
            bool hasRotation = Mathf.Abs(_rotationOffset) > _minCorrectionThreshold;

            if (hasPosition || hasRotation)
            {
                // Frame-rate-unabhaengiger exponentieller Decay.
                float dt60 = dt * 60f;
                float posFactor = Mathf.Pow(1f - _correctionRate, dt60);
                float rotFactor = Mathf.Pow(1f - _rotationCorrectionRate, dt60);

                _positionOffset *= posFactor;
                _rotationOffset *= rotFactor;

                // Micro-Jitter vermeiden
                if (_positionOffset.sqrMagnitude < _minCorrectionThreshold * _minCorrectionThreshold)
                    _positionOffset = Vector3.zero;
                if (Mathf.Abs(_rotationOffset) < _minCorrectionThreshold)
                    _rotationOffset = 0f;
            }

            // 4. Final Visual = Interpolated Position + Correction Offset
            Vector3 finalPos = interpPos + _positionOffset;
            Quaternion finalRot = _smoothRot * Quaternion.Euler(0f, _rotationOffset, 0f);

            transform.SetPositionAndRotation(finalPos, finalRot);

            // --- Diagnostics ---
            if (_debugLog)
            {
                Vector3 delta = finalPos - _diagLastFinalPos;

                // Stutter-Detection: Bewegungsdelta aendert sich stark zwischen Frames.
                const float minStutterDelta = 0.03f;
                if (_diagLastDelta.sqrMagnitude > minStutterDelta * minStutterDelta
                    && delta.sqrMagnitude > minStutterDelta * minStutterDelta
                    && _diagFrameCount > 3)
                {
                    float ratio = delta.magnitude / _diagLastDelta.magnitude;
                    if (ratio < 0.2f || ratio > 5f)
                    {
                        Debug.LogWarning($"[Smoother] STUTTER! delta={delta.magnitude:F4}m " +
                            $"prevDelta={_diagLastDelta.magnitude:F4}m ratio={ratio:F2} " +
                            $"dt={dt:F4}s " +
                            $"offset={_positionOffset.magnitude:F4}m " +
                            $"queue={_goalQueue.Count} t={_interpT:F3} " +
                            $"frame={_diagFrameCount}");
                    }
                }

                // Status alle 120 Frames
                if (_diagFrameCount % 120 == 0)
                {
                    Debug.Log($"[Smoother] Status frame={_diagFrameCount}: " +
                        $"pos={finalPos:F3} from={_fromPos:F3} to={_toPos:F3} " +
                        $"t={_interpT:F3} queue={_goalQueue.Count} " +
                        $"offset={_positionOffset:F4} |offset|={_positionOffset.magnitude:F4}m " +
                        $"fps={1f / dt:F0}");
                }

                _diagLastFinalPos = finalPos;
                _diagLastDelta = delta;
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (_positionOffset.sqrMagnitude < _minCorrectionThreshold * _minCorrectionThreshold)
                return;

            Gizmos.color = Color.yellow;
            Vector3 correctedTarget = transform.position - _positionOffset;
            Gizmos.DrawLine(transform.position, correctedTarget);
            Gizmos.DrawWireSphere(correctedTarget, 0.05f);
        }

        #endregion
    }
}
