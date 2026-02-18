using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Einheitliches Visual-Smoothing fuer Netzwerk-Characters.
    /// Handhabt BEIDES: Tick-Interpolation UND Reconcile-Correction.
    ///
    /// Verwendet One-Tick-Behind Interpolation:
    /// - Display zeigt immer die VORHERIGE Tick-Bewegung (einen Tick hinter der Simulation)
    /// - Dadurch hat die Interpolation eine VOLLE Tick-Dauer zum Erreichen von factor=1.0
    /// - Kein Velocity-Sprung an Tick-Grenzen (neuer displayStart = vorheriges displayEnd)
    /// - Multi-Tick-per-Frame wird graceful gehandhabt (kein Rangverlust)
    /// - Visueller Latenz-Overhead: ~1 Tick (33ms bei 30Hz)
    ///
    /// Ersetzt die KCC-eigene CustomInterpolationUpdate (CharacterMotorSystem.Settings.Interpolate = false).
    /// Damit gibt es nur EIN System das Transform.position in LateUpdate schreibt — kein Kaempfen.
    ///
    /// Buffer-Architektur (One-Tick-Behind):
    ///   displayStart ──Lerp──▶ displayEnd    (aktuell angezeigte Range, = vorheriger Tick)
    ///                           pendingEnd    (neueste Simulation, wird naechsten Tick angezeigt)
    ///
    /// Flow:
    /// 1. OnPreTick(): Buffer-Shift (pending → displayEnd → displayStart), Timing starten
    /// 2. [Replicate]: Simulation laeuft, TransientPosition aendert sich
    ///    → CharacterMotorSystem.Simulate() schreibt TransientPosition auf transform.position
    ///    → NetworkCharacterDriver stellt transform = TransientPosition VOR Simulate() sicher
    /// 3. OnReconcileComplete(): Bei Reconcile — Error zum Offset addieren, Buffer shiften
    /// 4. OnPostTick(): Pending-Position speichern (wird naechsten Tick displayed)
    /// 5. LateUpdate (Order 50): Interpoliert displayStart→displayEnd + decaying Offset
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

        [Header("Thresholds")]
        [Tooltip("Unter diesem Wert wird der Offset auf Zero gesetzt (verhindert Micro-Jitter).")]
        [SerializeField] private float _minCorrectionThreshold = 0.001f;

        [Header("Debug")]
        [Tooltip("Loggt Corrections die groesser als MinCorrectionThreshold sind.")]
        [SerializeField] private bool _debugLog;

        // --- One-Tick-Behind Buffer ---
        // Display interpolates between displayStart and displayEnd (= previous tick's range).
        // pendingEnd holds the latest simulation result (= current tick's end, displayed next tick).
        private Vector3 _displayStartPos;
        private Quaternion _displayStartRot;
        private Vector3 _displayEndPos;
        private Quaternion _displayEndRot;
        private Vector3 _pendingEndPos;
        private Quaternion _pendingEndRot;

        // --- Timing ---
        private float _interpStartTime;
        private float _interpDuration;
        private float _lastTickTime;
        private int _warmupTicks;
        private bool _initialized;
        private int _lastShiftFrame = -1;

        // --- Correction Offset ---
        private Vector3 _positionOffset;
        private float _rotationOffset;

        // --- Diagnostics ---
        private int _diagFrameCount;
        private Vector3 _diagLastFinalPos;
        private Vector3 _diagLastDelta;
        private float _diagLastInterpStartTime;
        private bool _diagStartupLogged;

        /// <summary>Snap-Threshold fuer externe Abfrage.</summary>
        public float SnapThreshold => _snapThreshold;

        /// <summary>Aktueller visueller Offset (fuer Debug).</summary>
        public Vector3 CurrentOffset => _positionOffset;

        /// <summary>Aktueller Rotations-Offset in Grad (fuer Debug).</summary>
        public float CurrentRotationOffset => _rotationOffset;

        /// <summary>Ob der Smoother aktiv interpoliert (nach 2 Ticks Warmup).</summary>
        public bool IsActive => _initialized;

        #region Tick Lifecycle

        /// <summary>
        /// Wird von NetworkCharacterDriver VOR dem Tick aufgerufen.
        /// Shiftet den Buffer: pending → displayEnd, displayEnd → displayStart.
        /// Startet das Interpolations-Timing fuer die neue Display-Range.
        ///
        /// Multi-Tick-per-Frame Guard:
        ///   Maximal EIN Buffer-Shift pro Render-Frame.
        ///   Wenn mehrere Ticks im selben Frame feuern (ParrelSync, Frame-Spikes),
        ///   wird nur der erste Shift ausgefuehrt. Folge-Ticks aktualisieren nur pendingEnd.
        ///   → Display springt NICHT um mehrere Ticks, sondern zeigt beim naechsten Frame
        ///     eine etwas groessere Range (2-3x Geschwindigkeit statt Positions-Sprung).
        ///
        /// Continuity-Beweis:
        ///   Vor Shift: visual bei factor≈1.0 = displayEnd
        ///   Nach Shift: displayStart = old displayEnd, factor=0 → visual = displayStart = old displayEnd
        ///   → Kein Sprung an der Tick-Grenze.
        /// </summary>
        public void OnPreTick(Vector3 motorPos, Quaternion motorRot, float tickDelta)
        {
            float now = Time.time;
            int frame = Time.frameCount;

            if (_warmupTicks == 0)
            {
                // Erster Tick: Alles auf aktuelle Position initialisieren.
                // Kein Display — wir brauchen mindestens 2 Ticks fuer eine Range.
                _displayStartPos = _displayEndPos = _pendingEndPos = motorPos;
                _displayStartRot = _displayEndRot = _pendingEndRot = motorRot;
                _lastTickTime = now;
                _lastShiftFrame = frame;
                _warmupTicks = 1;
                return;
            }

            // Multi-Tick-per-Frame Guard: Maximal EIN Shift pro Render-Frame.
            // Folge-Ticks im selben Frame skippen den Shift.
            // OnPostTick aktualisiert trotzdem pendingEnd — beim naechsten Frame-Shift
            // springt displayEnd auf die neueste Position (groessere Range, kein Positions-Sprung).
            if (frame == _lastShiftFrame && _initialized)
            {
                if (_debugLog)
                    Debug.Log($"[Smoother] Multi-tick skip: frame={frame} pos={motorPos:F3}");
                return;
            }
            _lastShiftFrame = frame;

            // Buffer-Shift: displayEnd → displayStart, pendingEnd → displayEnd
            _displayStartPos = _displayEndPos;
            _displayStartRot = _displayEndRot;
            _displayEndPos = _pendingEndPos;
            _displayEndRot = _pendingEndRot;

            // Timing: Interpolation laeuft von jetzt bis zum naechsten Tick.
            // Verwende die tatsaechliche Zeit seit letztem Tick (adaptiv an Frame-Timing).
            // Damit erreicht factor≈1.0 exakt wenn der naechste Tick feuert.
            float rawDuration = now - _lastTickTime;
            _interpDuration = (rawDuration > 0.001f) ? rawDuration : tickDelta;
            _interpStartTime = now;
            _lastTickTime = now;

            if (_warmupTicks == 1)
            {
                _warmupTicks = 2;
                _initialized = true;
            }
        }

        /// <summary>
        /// Wird von NetworkCharacterDriver NACH dem Tick aufgerufen.
        /// Speichert die Post-Simulation Position als Pending (wird naechsten Tick displayed).
        /// </summary>
        public void OnPostTick(Vector3 motorPos, Quaternion motorRot, float tickDelta)
        {
            _pendingEndPos = motorPos;
            _pendingEndRot = motorRot;

            // Sofort korrekte visuelle Position setzen.
            // Verhindert dass Animator, IK oder andere Systeme zwischen OnPostTick und LateUpdate
            // die rohe Simulations-Position sehen.
            // factor=0 (interpStartTime = Time.time = jetzt), visual = displayStart + offset.
            if (_initialized)
            {
                Vector3 visualPos = _displayStartPos + _positionOffset;
                Quaternion visualRot = _displayStartRot * Quaternion.Euler(0f, _rotationOffset, 0f);
                transform.SetPositionAndRotation(visualPos, visualRot);
            }
        }

        #endregion

        #region Reconcile Correction

        /// <summary>
        /// Wird nach Owner-Reconcile+Replay aufgerufen (in PerformReplicate, ContainsTicked).
        /// Berechnet Error, akkumuliert Offset, und shiftet den Display-Buffer.
        ///
        /// Buffer-Shift bei Correction:
        ///   displayStart/End werden um den Korrekturvektor verschoben.
        ///   Offset wird um den Error-Vektor erhoeht (= Gegenrichtung).
        ///   → Visual bleibt EXAKT gleich (Shift + Offset heben sich auf).
        ///   → Beim Decay des Offsets gleitet das Visual zur korrigierten Trajektorie.
        /// </summary>
        public void OnReconcileComplete(Vector3 preReconcilePos, float preReconcileRotY,
                                         Vector3 correctedPos, float correctedRotY)
        {
            Vector3 posError = preReconcilePos - correctedPos;
            float rotError = Mathf.DeltaAngle(correctedRotY, preReconcileRotY);

            if (posError.sqrMagnitude > _snapThreshold * _snapThreshold)
            {
                // Hard snap: Display auf korrigierte Position setzen
                _displayStartPos = _displayEndPos = _pendingEndPos = correctedPos;
                Quaternion corrRot = Quaternion.Euler(0f, correctedRotY, 0f);
                _displayStartRot = _displayEndRot = _pendingEndRot = corrRot;
                ClearOffset();
            }
            else
            {
                // Smooth correction: Display-Buffer shiften + Offset akkumulieren.
                // correction = -posError (Richtung von pre nach corrected)
                Vector3 correction = correctedPos - preReconcilePos;
                _displayStartPos += correction;
                _displayEndPos += correction;

                // Rotation analog: Display-Rotationen um Korrektur drehen
                Quaternion rotCorrection = Quaternion.Euler(0f, -rotError, 0f);
                _displayStartRot = rotCorrection * _displayStartRot;
                _displayEndRot = rotCorrection * _displayEndRot;

                _positionOffset += posError;
                _rotationOffset += rotError;
            }

            if (_debugLog && posError.sqrMagnitude > _minCorrectionThreshold * _minCorrectionThreshold)
                Debug.Log($"[ReconcileSmoother] Reconcile: pos={posError.magnitude:F4}m rot={rotError:F2}°");
        }

        /// <summary>
        /// Wird nach Spectator-Correction aufgerufen (nach Simulation mit neuem autoritativem Input).
        /// Gleiche Buffer-Shift Logik wie OnReconcileComplete.
        /// </summary>
        public void OnSpectatorCorrection(Vector3 prePos, Vector3 postPos, Quaternion postRot)
        {
            Vector3 error = prePos - postPos;

            if (error.sqrMagnitude > _snapThreshold * _snapThreshold)
            {
                _displayStartPos = _displayEndPos = _pendingEndPos = postPos;
                _displayStartRot = _displayEndRot = _pendingEndRot = postRot;
                ClearOffset();
            }
            else
            {
                Vector3 correction = postPos - prePos;
                _displayStartPos += correction;
                _displayEndPos += correction;

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
            _initialized = false;
            _warmupTicks = 0;
            _lastShiftFrame = -1;
        }

        /// <summary>
        /// Setzt den Multi-Tick-per-Frame Guard zurueck.
        /// Nur fuer Unit Tests noetig, da Time.frameCount dort nicht zwischen
        /// simulierten Ticks inkrementiert.
        /// </summary>
        public void ResetFrameGuard() => _lastShiftFrame = -1;

        #endregion

        #region Visual Update

        private void LateUpdate()
        {
            // Offline-Guard: Ohne 2 Ticks Warmup kein Smoothing.
            // (KCC handhabt Interpolation selbst via CustomInterpolationUpdate)
            if (!_initialized) return;

            // Startup-Log (einmalig)
            if (_debugLog && !_diagStartupLogged)
            {
                _diagStartupLogged = true;
                Debug.Log($"[Smoother] ACTIVE on {gameObject.name} | " +
                    $"interpDuration={_interpDuration:F4}s snapThreshold={_snapThreshold}m " +
                    $"correctionRate={_correctionRate} mode=OneTickBehind");
            }

            _diagFrameCount++;

            // 1. Tick-Interpolation (One-Tick-Behind: displayStart → displayEnd)
            float factor = (_interpDuration > 0f)
                ? Mathf.Clamp01((Time.time - _interpStartTime) / _interpDuration)
                : 1f;

            Vector3 interpPos = Vector3.Lerp(_displayStartPos, _displayEndPos, factor);
            Quaternion interpRot = Quaternion.Slerp(_displayStartRot, _displayEndRot, factor);

            // 2. Offset-Decay
            bool hasPosition = _positionOffset.sqrMagnitude > _minCorrectionThreshold * _minCorrectionThreshold;
            bool hasRotation = Mathf.Abs(_rotationOffset) > _minCorrectionThreshold;

            if (hasPosition || hasRotation)
            {
                // Frame-rate-unabhaengiger exponentieller Decay.
                float dt = Time.deltaTime * 60f;
                float posFactor = Mathf.Pow(1f - _correctionRate, dt);
                float rotFactor = Mathf.Pow(1f - _rotationCorrectionRate, dt);

                _positionOffset *= posFactor;
                _rotationOffset *= rotFactor;

                // Micro-Jitter vermeiden
                if (_positionOffset.sqrMagnitude < _minCorrectionThreshold * _minCorrectionThreshold)
                    _positionOffset = Vector3.zero;
                if (Mathf.Abs(_rotationOffset) < _minCorrectionThreshold)
                    _rotationOffset = 0f;
            }

            // 3. Final Visual = Interpolation + Correction Offset
            Vector3 finalPos = interpPos + _positionOffset;
            Quaternion finalRot = interpRot * Quaternion.Euler(0f, _rotationOffset, 0f);

            // Diagnostics: Tamper-Detection
            if (_debugLog && _diagFrameCount > 1)
            {
                Vector3 expectedPos = _diagLastFinalPos;
                Vector3 actualPos = transform.position;
                Vector3 tamperDelta = actualPos - expectedPos;
                if (tamperDelta.sqrMagnitude > 0.0001f * 0.0001f) // 0.1mm
                {
                    bool tickThisFrame = _interpStartTime != _diagLastInterpStartTime;
                    if (!tickThisFrame)
                    {
                        Debug.LogWarning($"[Smoother] TAMPERED! delta={tamperDelta.magnitude:F4}m " +
                            $"expected={expectedPos:F3} actual={actualPos:F3}");
                    }
                }
            }

            transform.SetPositionAndRotation(finalPos, finalRot);

            // --- JITTER DETECTION ---
            if (_debugLog)
            {
                Vector3 delta = finalPos - _diagLastFinalPos;
                bool tickThisFrame = _interpStartTime != _diagLastInterpStartTime;

                // Stillstand-Jitter: Position aendert sich obwohl displayStart==displayEnd und offset==0
                bool shouldBeStill = (_displayStartPos - _displayEndPos).sqrMagnitude < 0.00001f
                                     && _positionOffset.sqrMagnitude < _minCorrectionThreshold * _minCorrectionThreshold;
                if (shouldBeStill && delta.sqrMagnitude > 0.00001f && _diagFrameCount > 2)
                {
                    Debug.LogWarning($"[Smoother] STILL-JITTER! delta={delta.magnitude:F6}m " +
                        $"factor={factor:F3} tick={tickThisFrame} frame={_diagFrameCount}");
                }

                // Stutter-Detection: Bewegungsdelta aendert sich stark zwischen non-tick Frames
                if (!shouldBeStill && _diagLastDelta.sqrMagnitude > 0.00001f
                    && delta.sqrMagnitude > 0.00001f && !tickThisFrame)
                {
                    float ratio = delta.magnitude / _diagLastDelta.magnitude;
                    if (ratio < 0.3f || ratio > 3f)
                    {
                        Debug.LogWarning($"[Smoother] STUTTER! delta={delta.magnitude:F4}m " +
                            $"prevDelta={_diagLastDelta.magnitude:F4}m ratio={ratio:F2} " +
                            $"factor={factor:F3} frame={_diagFrameCount}");
                    }
                }

                // Status alle 120 Frames
                if (_diagFrameCount % 120 == 0)
                {
                    Debug.Log($"[Smoother] Status frame={_diagFrameCount}: " +
                        $"pos={finalPos:F3} start={_displayStartPos:F3} end={_displayEndPos:F3} " +
                        $"pending={_pendingEndPos:F3} factor={factor:F3} offset={_positionOffset:F4} " +
                        $"interpDur={_interpDuration:F4}s fps={1f / Time.deltaTime:F0}");
                }

                _diagLastFinalPos = finalPos;
                _diagLastDelta = delta;
                _diagLastInterpStartTime = _interpStartTime;
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
