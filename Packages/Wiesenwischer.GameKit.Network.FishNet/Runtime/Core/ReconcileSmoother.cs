using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Einheitliches Visual-Smoothing fuer Netzwerk-Characters.
    /// Handhabt BEIDES: Tick-Interpolation UND Reconcile-Correction.
    ///
    /// Verwendet Dead Reckoning + Drift-Korrektur:
    /// - Jeden Frame: _smoothPos += _targetVelocity * deltaTime (konstante Geschwindigkeit)
    /// - Drift-Korrektur: sanftes Ziehen Richtung Simulation (verhindert Divergenz)
    /// - Ergebnis: gleichmaessige Deltas proportional zu deltaTime, unabhaengig von Tick-Verteilung
    ///
    /// Warum nicht SmoothDamp/Prediction:
    ///   SmoothDamp verfolgt ein Target. Auf Tick-Frames springt das Target (tick fires),
    ///   auf Nicht-Tick-Frames bleibt es statisch. SmoothDamp mit kleinem smoothTime
    ///   (&lt; frameTime) snappt quasi zum Target → visuelles Oszillieren.
    ///
    ///   Prediction extrapoliert das Target per Velocity, aber Time.time ist in OnPostTick
    ///   und LateUpdate identisch → timeSinceUpdate=0 auf Tick-Frames, ~0.03s auf Nicht-Tick-Frames.
    ///   Das Target oszilliert zwischen _targetPos und _targetPos + velocity*dt → noch schlimmer.
    ///
    /// Dead Reckoning vermeidet das Problem:
    ///   _smoothPos += velocity * deltaTime produziert IMMER einen proportionalen Schritt,
    ///   egal ob ein Tick gefeuert hat oder nicht. Die Drift-Korrektur ist sanft genug
    ///   (Rate=2/s → ~6% des Drifts pro Frame) um keine sichtbare Perturbation zu erzeugen.
    ///
    /// Architektur:
    ///   _targetPos      = neueste Post-Tick Motor-Position (wird jeden Tick aktualisiert)
    ///   _targetVelocity = Geschwindigkeit zwischen letzten beiden Ticks (fuer Dead Reckoning)
    ///   _smoothPos      = aktuelle visuelle Position (Dead Reckoning + Drift-Korrektur)
    ///   _positionOffset = Reconcile-Correction Offset (exponentieller Decay)
    ///
    /// Visual = _smoothPos + _positionOffset
    ///
    /// Ersetzt die KCC-eigene CustomInterpolationUpdate (CharacterMotorSystem.Settings.Interpolate = false).
    /// Damit gibt es nur EIN System das Transform.position in LateUpdate schreibt — kein Kaempfen.
    ///
    /// Flow:
    /// 1. OnPreTick(): Initialisierung (einmalig beim ersten Tick)
    /// 2. [Replicate]: Simulation laeuft, TransientPosition aendert sich
    /// 3. OnReconcileComplete(): Bei Reconcile — Error zum Offset addieren, _smoothPos shiften
    /// 4. OnPostTick(): Target + Velocity aktualisieren (neueste Simulation)
    /// 5. LateUpdate (Order 50): Dead Reckoning, Drift-Korrektur, Offset-Decay
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

        [Header("Smoothing")]
        [Tooltip("Drift-Korrektur Rate pro Sekunde. " +
                 "Hoehere Werte = schnellere Korrektur aber weniger smooth. " +
                 "2.0 = ~6% Korrektur pro Frame bei 30fps.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _driftCorrectionRate = 2f;

        [Header("Thresholds")]
        [Tooltip("Unter diesem Wert wird der Offset auf Zero gesetzt (verhindert Micro-Jitter).")]
        [SerializeField] private float _minCorrectionThreshold = 0.001f;

        [Header("Debug")]
        [Tooltip("Loggt Corrections die groesser als MinCorrectionThreshold sind.")]
        [SerializeField] private bool _debugLog;

        // --- Target (latest post-tick motor position) ---
        private Vector3 _targetPos;
        private Quaternion _targetRot;

        // --- Dead Reckoning ---
        // Velocity aus konsekutiven Ticks, fuer frame-unabhaengige Bewegung.
        private Vector3 _prevTargetPos;
        private Vector3 _targetVelocity;
        private float _targetAngularVelocityY;
        private float _tickDelta;

        // --- Visual (Dead Reckoning state) ---
        private Vector3 _smoothPos;
        private Quaternion _smoothRot;

        // --- Correction Offset ---
        private Vector3 _positionOffset;
        private float _rotationOffset;

        // --- State ---
        private bool _initialized;

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
                _targetPos = _prevTargetPos = _smoothPos = motorPos;
                _targetRot = _smoothRot = motorRot;
                _targetVelocity = Vector3.zero;
                _targetAngularVelocityY = 0f;
                _initialized = true;

                if (_debugLog)
                    Debug.Log($"[Smoother] Initialized at {motorPos:F3}, tickDelta={tickDelta:F4}s");
            }
        }

        /// <summary>
        /// Wird von NetworkCharacterDriver NACH dem Tick aufgerufen.
        /// Aktualisiert Target-Position und berechnet Tick-Velocity fuer Dead Reckoning.
        /// Setzt Transform auf die aktuelle visuelle Position (verhindert dass Animator/IK
        /// die rohe Simulations-Position sehen).
        /// </summary>
        public void OnPostTick(Vector3 motorPos, Quaternion motorRot, float tickDelta)
        {
            // Velocity aus konsekutiven Tick-Positionen berechnen.
            // Bei mehreren Ticks pro Frame: nur der letzte Tick zaehlt (hoechste Aktualitaet).
            if (tickDelta > 0f)
            {
                _targetVelocity = (motorPos - _prevTargetPos) / tickDelta;

                // Rotations-Velocity: Y-Rotation Delta pro Tick
                float prevY = _targetRot.eulerAngles.y;
                float currY = motorRot.eulerAngles.y;
                _targetAngularVelocityY = Mathf.DeltaAngle(prevY, currY) / tickDelta;
            }

            _prevTargetPos = motorPos;
            _targetPos = motorPos;
            _targetRot = motorRot;
            _tickDelta = tickDelta;

            // Sofort korrekte visuelle Position setzen.
            // Verhindert dass Animator, IK oder andere Systeme zwischen OnPostTick und LateUpdate
            // die rohe Simulations-Position sehen.
            if (_initialized)
            {
                transform.SetPositionAndRotation(
                    _smoothPos + _positionOffset,
                    _smoothRot * Quaternion.Euler(0f, _rotationOffset, 0f));
            }
        }

        #endregion

        #region Reconcile Correction

        /// <summary>
        /// Wird nach Owner-Reconcile+Replay aufgerufen (in PerformReplicate, ContainsTicked).
        /// Berechnet Error, akkumuliert Offset, und shiftet _smoothPos.
        ///
        /// Visual-Stabilitaet:
        ///   _smoothPos += correction (gleiche Richtung wie Korrektur)
        ///   _positionOffset += error (Gegenrichtung)
        ///   → Visual bleibt EXAKT gleich: (smoothPos + correction) + (offset + error) = smoothPos + offset
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
                _targetPos = _prevTargetPos = _smoothPos = correctedPos;
                Quaternion corrRot = Quaternion.Euler(0f, correctedRotY, 0f);
                _targetRot = _smoothRot = corrRot;
                _targetVelocity = Vector3.zero;
                _targetAngularVelocityY = 0f;
                ClearOffset();
            }
            else
            {
                // Smooth correction: _smoothPos shiften + Offset akkumulieren.
                // correction = corrected - preReconcile (Richtung von alt zu neu)
                Vector3 correction = correctedPos - preReconcilePos;
                _smoothPos += correction;

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
        /// Gleiche Logik wie OnReconcileComplete.
        /// </summary>
        public void OnSpectatorCorrection(Vector3 prePos, Vector3 postPos, Quaternion postRot)
        {
            Vector3 error = prePos - postPos;

            if (error.sqrMagnitude > _snapThreshold * _snapThreshold)
            {
                _targetPos = _prevTargetPos = _smoothPos = postPos;
                _targetRot = _smoothRot = postRot;
                _targetVelocity = Vector3.zero;
                _targetAngularVelocityY = 0f;
                ClearOffset();
            }
            else
            {
                Vector3 correction = postPos - prePos;
                _smoothPos += correction;

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
            _targetVelocity = Vector3.zero;
            _targetAngularVelocityY = 0f;
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
                    $"correctionRate={_correctionRate} driftRate={_driftCorrectionRate}/s " +
                    $"mode=DeadReckoning");
            }

            _diagFrameCount++;
            float dt = Time.deltaTime;

            // 1. Dead Reckoning: Visual per Velocity bewegen.
            //    Produziert IMMER einen proportionalen Schritt (velocity * deltaTime),
            //    unabhaengig davon ob ein Tick gefeuert hat.
            //    → Kein Stutter bei alternierenden 0-Tick/2-Tick Frames.
            _smoothPos += _targetVelocity * dt;

            // Rotation: Dead Reckoning per Angular Velocity
            _smoothRot *= Quaternion.Euler(0f, _targetAngularVelocityY * dt, 0f);

            // 2. Drift-Korrektur: Sanft Richtung Simulation ziehen.
            //    Verhindert Divergenz bei Geschwindigkeitsaenderungen, Kurven, etc.
            //    Exponentieller Decay: alpha = 1 - exp(-dt * rate)
            //    Bei rate=2, dt=0.030: alpha ≈ 0.058 → ~6% des Drifts pro Frame.
            float driftAlpha = 1f - Mathf.Exp(-dt * _driftCorrectionRate);

            Vector3 posDrift = _targetPos - _smoothPos;
            _smoothPos += posDrift * driftAlpha;

            _smoothRot = Quaternion.Slerp(_smoothRot, _targetRot, driftAlpha);

            // 3. Offset-Decay (Reconcile Correction)
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

            // 4. Final Visual = Smooth Position + Correction Offset
            Vector3 finalPos = _smoothPos + _positionOffset;
            Quaternion finalRot = _smoothRot * Quaternion.Euler(0f, _rotationOffset, 0f);

            transform.SetPositionAndRotation(finalPos, finalRot);

            // --- Diagnostics ---
            if (_debugLog)
            {
                Vector3 delta = finalPos - _diagLastFinalPos;
                Vector3 drift = _targetPos - _smoothPos;

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
                            $"dt={dt:F4}s drift={drift.magnitude:F3}m " +
                            $"frame={_diagFrameCount}");
                    }
                }

                // Status alle 120 Frames
                if (_diagFrameCount % 120 == 0)
                {
                    Debug.Log($"[Smoother] Status frame={_diagFrameCount}: " +
                        $"pos={finalPos:F3} smooth={_smoothPos:F3} target={_targetPos:F3} " +
                        $"vel={_targetVelocity:F3} drift={drift:F3} " +
                        $"offset={_positionOffset:F4} " +
                        $"driftRate={_driftCorrectionRate}/s fps={1f / dt:F0}");
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
