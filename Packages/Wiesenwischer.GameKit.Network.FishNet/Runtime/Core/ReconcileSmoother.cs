using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Einheitliches Visual-Smoothing fuer Netzwerk-Characters.
    /// Handhabt BEIDES: Tick-Interpolation UND Reconcile-Correction.
    ///
    /// Verwendet SmoothDamp statt Linear-Lerp fuer Tick-Interpolation:
    /// - Velocity-Kontinuitaet an Tick-Grenzen (kein Speed-Sprung)
    /// - Automatisch smooth bei variablen Frame-Raten und Multi-Tick-per-Frame
    /// - Kein Buffer-Shift, kein Multi-Tick-Guard noetig
    ///
    /// Architektur:
    ///   _targetPos  = neueste Post-Tick Motor-Position (wird jeden Tick aktualisiert)
    ///   _smoothPos  = aktuelle visuelle Position (SmoothDamp verfolgt _targetPos)
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
    /// 4. OnPostTick(): Target-Position aktualisieren (neueste Simulation)
    /// 5. LateUpdate (Order 50): SmoothDamp _smoothPos → _targetPos + decaying Offset
    ///    → Laeuft VOR CameraBrain (100) und GroundingSmoother (100)
    ///
    /// Warum SmoothDamp statt Lerp:
    ///   Linearer Lerp(displayStart, displayEnd, factor) erzeugt Velocity-Sprünge an Tick-Grenzen.
    ///   Bei non-integer Frames/Tick (z.B. 100fps/30Hz = 3.33 Frames) erreicht der Factor nie
    ///   genau 1.0 vor dem Shift. Der Rest (z.B. 0.1 * range) wird im Tick-Frame traversiert,
    ///   waehrend normale Frames 0.3 * range traversieren → sichtbarer Stutter.
    ///   SmoothDamp verfolgt das Target mit kontinuierlicher Velocity → kein Sprung.
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

            if (!_initialized)
            {
                _targetPos = _smoothPos = motorPos;
                _targetRot = _smoothRot = motorRot;
                _smoothVelocity = Vector3.zero;
                _initialized = true;

                if (_debugLog)
                    Debug.Log($"[Smoother] Initialized at {motorPos:F3}, smoothTime={_smoothTime:F4}s");
            }
        }

        /// <summary>
        /// Wird von NetworkCharacterDriver NACH dem Tick aufgerufen.
        /// Aktualisiert die Target-Position (neueste Simulation).
        /// Setzt Transform auf die aktuelle visuelle Position (verhindert dass Animator/IK
        /// die rohe Simulations-Position sehen).
        /// </summary>
        public void OnPostTick(Vector3 motorPos, Quaternion motorRot, float tickDelta)
        {
            _targetPos = motorPos;
            _targetRot = motorRot;
            _smoothTime = tickDelta * _smoothTimeFactor;

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
                _targetPos = _smoothPos = correctedPos;
                Quaternion corrRot = Quaternion.Euler(0f, correctedRotY, 0f);
                _targetRot = _smoothRot = corrRot;
                _smoothVelocity = Vector3.zero;
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
                _targetPos = _smoothPos = postPos;
                _targetRot = _smoothRot = postRot;
                _smoothVelocity = Vector3.zero;
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
                    $"correctionRate={_correctionRate} mode=SmoothDamp");
            }

            _diagFrameCount++;

            // 1. SmoothDamp Position toward target
            //    Bietet Velocity-Kontinuitaet: kein Speed-Sprung wenn _targetPos sich aendert.
            //    maxSpeed=Infinity: keine kuenstliche Geschwindigkeitsbegrenzung.
            float safeSmoothTime = Mathf.Max(_smoothTime, 0.001f);
            _smoothPos = Vector3.SmoothDamp(
                _smoothPos, _targetPos, ref _smoothVelocity,
                safeSmoothTime, Mathf.Infinity, Time.deltaTime);

            // 2. Exponential Rotation smoothing toward target
            //    Slerp mit exponentialem Alpha: smooth ohne Overshoot.
            float rotAlpha = 1f - Mathf.Exp(-Time.deltaTime / safeSmoothTime);
            _smoothRot = Quaternion.Slerp(_smoothRot, _targetRot, rotAlpha);

            // 3. Offset-Decay
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

            // 4. Final Visual = Smooth Position + Correction Offset
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
                            $"frame={_diagFrameCount}");
                    }
                }

                // Status alle 120 Frames
                if (_diagFrameCount % 120 == 0)
                {
                    Debug.Log($"[Smoother] Status frame={_diagFrameCount}: " +
                        $"pos={finalPos:F3} smooth={_smoothPos:F3} target={_targetPos:F3} " +
                        $"vel={_smoothVelocity:F3} offset={_positionOffset:F4} " +
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
