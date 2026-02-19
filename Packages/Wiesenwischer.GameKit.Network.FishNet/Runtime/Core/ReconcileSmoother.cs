using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Einheitliches Visual-Smoothing fuer Netzwerk-Characters.
    /// Handhabt BEIDES: Tick-Interpolation UND Reconcile-Correction.
    ///
    /// Verwendet Velocity-Prediction + SmoothDamp:
    /// - Zwischen Ticks: Target wird per bekannter Velocity extrapoliert → gleichmaessige Bewegung
    /// - SmoothDamp verfolgt das extrapolierte Target → Velocity-Kontinuitaet
    /// - Kein Stutter bei ungleichmaessiger Tick-Verteilung (0-Tick vs 2-Tick Frames)
    ///
    /// Problem ohne Prediction:
    ///   Bei ParrelSync alternieren Frame-Zeiten (z.B. 20ms/46ms).
    ///   FishNet akkumuliert Zeit → alternierend 0 und 2 Ticks pro Frame.
    ///   Ohne Prediction: 0-Tick-Frame → Target statisch → SmoothDamp kaum Bewegung (0.05m)
    ///                     2-Tick-Frame → Target springt 0.4m → SmoothDamp grosser Schritt (0.25m)
    ///   → Sichtbares Alternieren (5:1 Ratio) = Stutter.
    ///
    /// Loesung:
    ///   Zwischen Ticks das Target per letzter Tick-Velocity extrapolieren:
    ///     predictedTarget = _targetPos + _targetVelocity * timeSinceLastTick
    ///   SmoothDamp jagt ein GLEICHMAESSIG BEWEGTES Target → konstante Deltas.
    ///   Bei Tick-Update: Prediction wird auf echte Position korrigiert (nahtlos).
    ///
    /// Architektur:
    ///   _targetPos      = neueste Post-Tick Motor-Position (wird jeden Tick aktualisiert)
    ///   _targetVelocity = Geschwindigkeit zwischen letzten beiden Ticks (fuer Extrapolation)
    ///   _smoothPos      = aktuelle visuelle Position (SmoothDamp verfolgt predicted Target)
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
    /// 5. LateUpdate (Order 50): Extrapolate Target, SmoothDamp, Offset-Decay
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
        [Tooltip("SmoothDamp smoothTime als Faktor der Tick-Dauer. " +
                 "0.5 = halbe Tick-Dauer (~16ms bei 30Hz). " +
                 "Kleinere Werte = weniger Lag, hoehere Werte = weicheres Smoothing.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _smoothTimeFactor = 0.5f;

        [Header("Thresholds")]
        [Tooltip("Unter diesem Wert wird der Offset auf Zero gesetzt (verhindert Micro-Jitter).")]
        [SerializeField] private float _minCorrectionThreshold = 0.001f;

        [Header("Debug")]
        [Tooltip("Loggt Corrections die groesser als MinCorrectionThreshold sind.")]
        [SerializeField] private bool _debugLog;

        // --- Target (latest post-tick motor position) ---
        private Vector3 _targetPos;
        private Quaternion _targetRot;

        // --- Velocity Prediction ---
        // Extrapoliert _targetPos zwischen Ticks, damit SmoothDamp ein
        // gleichmaessig bewegtes Target verfolgt statt eines stationaeren.
        private Vector3 _prevTargetPos;
        private Vector3 _targetVelocity;
        private Quaternion _targetAngularVelocity;
        private float _lastTargetUpdateTime;
        private float _tickDelta;

        // --- Visual (SmoothDamp state) ---
        private Vector3 _smoothPos;
        private Quaternion _smoothRot;
        private Vector3 _smoothVelocity;
        private float _smoothTime;

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
            _smoothTime = tickDelta * _smoothTimeFactor;
            _tickDelta = tickDelta;

            if (!_initialized)
            {
                _targetPos = _prevTargetPos = _smoothPos = motorPos;
                _targetRot = _smoothRot = motorRot;
                _targetVelocity = Vector3.zero;
                _targetAngularVelocity = Quaternion.identity;
                _smoothVelocity = Vector3.zero;
                _lastTargetUpdateTime = Time.time;
                _initialized = true;

                if (_debugLog)
                    Debug.Log($"[Smoother] Initialized at {motorPos:F3}, smoothTime={_smoothTime:F4}s");
            }
        }

        /// <summary>
        /// Wird von NetworkCharacterDriver NACH dem Tick aufgerufen.
        /// Aktualisiert Target-Position und berechnet Tick-Velocity fuer Extrapolation.
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

                // Rotations-Velocity: Delta-Rotation pro Tick
                _targetAngularVelocity = motorRot * Quaternion.Inverse(_targetRot);
            }

            _prevTargetPos = motorPos;
            _targetPos = motorPos;
            _targetRot = motorRot;
            _smoothTime = tickDelta * _smoothTimeFactor;
            _tickDelta = tickDelta;
            _lastTargetUpdateTime = Time.time;

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
                _smoothVelocity = Vector3.zero;
                _targetVelocity = Vector3.zero;
                _targetAngularVelocity = Quaternion.identity;
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
                _smoothVelocity = Vector3.zero;
                _targetVelocity = Vector3.zero;
                _targetAngularVelocity = Quaternion.identity;
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
            _smoothVelocity = Vector3.zero;
            _targetVelocity = Vector3.zero;
            _targetAngularVelocity = Quaternion.identity;
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
                    $"smoothTime={_smoothTime:F4}s snapThreshold={_snapThreshold}m " +
                    $"correctionRate={_correctionRate} mode=SmoothDamp+VelocityPrediction");
            }

            _diagFrameCount++;

            // 1. Velocity Prediction: Target zwischen Ticks extrapolieren.
            //    Auf 0-Tick-Frames bewegt sich das predicted Target weiter statt statisch zu sein.
            //    → SmoothDamp erzeugt gleichmaessige Deltas statt alternierend 0.05/0.25m.
            //    Cap bei 2×tickDelta (verhindert Ueber-Extrapolation bei Frame-Spikes).
            float timeSinceUpdate = Mathf.Min(
                Time.time - _lastTargetUpdateTime,
                _tickDelta * 2f);
            Vector3 predictedTarget = _targetPos + _targetVelocity * timeSinceUpdate;

            // Rotation: Extrapolation per Slerp-Anteil
            float rotPredictionFactor = (_tickDelta > 0f) ? timeSinceUpdate / _tickDelta : 0f;
            Quaternion predictedRot = _targetRot * Quaternion.Slerp(
                Quaternion.identity, _targetAngularVelocity, rotPredictionFactor);

            // 2. SmoothDamp Position toward predicted target
            //    Bietet Velocity-Kontinuitaet: kein Speed-Sprung wenn Target sich aendert.
            float safeSmoothTime = Mathf.Max(_smoothTime, 0.001f);
            _smoothPos = Vector3.SmoothDamp(
                _smoothPos, predictedTarget, ref _smoothVelocity,
                safeSmoothTime, Mathf.Infinity, Time.deltaTime);

            // 3. Exponential Rotation smoothing toward predicted rotation
            float rotAlpha = 1f - Mathf.Exp(-Time.deltaTime / safeSmoothTime);
            _smoothRot = Quaternion.Slerp(_smoothRot, predictedRot, rotAlpha);

            // 4. Offset-Decay
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

            // 5. Final Visual = Smooth Position + Correction Offset
            Vector3 finalPos = _smoothPos + _positionOffset;
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
                            $"dt={Time.deltaTime:F4}s tSinceUpd={timeSinceUpdate:F4}s " +
                            $"frame={_diagFrameCount}");
                    }
                }

                // Status alle 120 Frames
                if (_diagFrameCount % 120 == 0)
                {
                    Debug.Log($"[Smoother] Status frame={_diagFrameCount}: " +
                        $"pos={finalPos:F3} smooth={_smoothPos:F3} target={_targetPos:F3} " +
                        $"predicted={predictedTarget:F3} vel={_targetVelocity:F3} " +
                        $"offset={_positionOffset:F4} " +
                        $"smoothTime={_smoothTime:F4}s fps={1f / Time.deltaTime:F0}");
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
