using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Einheitliches Visual-Smoothing fuer Netzwerk-Characters.
    /// Handhabt BEIDES: Tick-Interpolation UND Reconcile-Correction.
    ///
    /// Verwendet Exponential Smoothing:
    /// - Jeden Frame: _smoothPos = Lerp(_smoothPos, _targetPos, alpha)
    ///   wobei alpha = 1 - exp(-deltaTime / smoothTime)
    /// - Erzeugt gleichmaessige visuelle Geschwindigkeit proportional zur Distanz
    /// - Kein Stoppen, kein Sprinten, keine Velocity-Berechnung
    ///
    /// Warum nicht MoveTowards:
    ///   MoveTowards clampt am Ziel → Visual stoppt, wartet auf naechsten Tick,
    ///   sprintet dann zum neuen Target. Da OnPostTick und LateUpdate im SELBEN
    ///   Unity-Frame laufen, bewegt sich das Visual ~96% der Strecke auf Tick-Frames
    ///   und ~4% auf Nicht-Tick-Frames → sichtbares 10:1 Stutter-Pattern.
    ///
    /// Exponential Smoothing vermeidet das:
    ///   Die visuelle Geschwindigkeit ist IMMER proportional zur Distanz zum Target.
    ///   Auf Tick-Frames steigt die Distanz (neues Target), auf Nicht-Tick-Frames
    ///   sinkt sie (Visual naehert sich). Die resultierende Geschwindigkeit aendert
    ///   sich sanft → gleichmaessige Frame-Deltas.
    ///
    ///   Steady-State Lag: ~speed * smoothTime. Bei smoothTime=0.033s und 2m/s = 6.6cm.
    ///   Bei smoothTime=0.033s und 5m/s = 16.5cm. Visuell nicht wahrnehmbar.
    ///
    /// Architektur:
    ///   _targetPos      = neueste Post-Tick Motor-Position (wird jeden Tick aktualisiert)
    ///   _smoothPos      = aktuelle visuelle Position (exponential approach)
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
    /// 4. OnPostTick(): Target aktualisieren
    /// 5. LateUpdate (Order 50): Exponential Smoothing, Offset-Decay
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

        [Header("Interpolation")]
        [Tooltip("Zeitkonstante fuer Exponential Smoothing in Sekunden.\n" +
                 "Hoeher = smoother aber mehr visueller Lag.\n" +
                 "Empfohlen: 1-2× tickDelta (0.017-0.033 bei 60Hz).\n" +
                 "Lag ≈ Geschwindigkeit × SmoothTime (bei 5m/s und 0.033s = 16cm).")]
        [Range(0.01f, 0.1f)]
        [SerializeField] private float _interpSmoothTime = 0.033f;

        [Header("Thresholds")]
        [Tooltip("Unter diesem Wert wird der Offset auf Zero gesetzt (verhindert Micro-Jitter).")]
        [SerializeField] private float _minCorrectionThreshold = 0.001f;

        [Header("Debug")]
        [Tooltip("Loggt Corrections die groesser als MinCorrectionThreshold sind.")]
        [SerializeField] private bool _debugLog;

        // --- Target (latest post-tick motor position) ---
        private Vector3 _targetPos;
        private Quaternion _targetRot;

        // --- Visual (exponential smoothing state) ---
        private Vector3 _smoothPos;
        private Quaternion _smoothRot;

        // --- Correction Offset ---
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
                _targetPos = _smoothPos = motorPos;
                _targetRot = _smoothRot = motorRot;
                _initialized = true;

                if (_debugLog)
                    Debug.Log($"[Smoother] Initialized at {motorPos:F3}, tickDelta={tickDelta:F4}s");
            }
        }

        /// <summary>
        /// Wird von NetworkCharacterDriver NACH dem Tick aufgerufen.
        /// Aktualisiert Target-Position.
        /// Setzt Transform auf die aktuelle visuelle Position (verhindert dass Animator/IK
        /// die rohe Simulations-Position sehen).
        /// </summary>
        public void OnPostTick(Vector3 motorPos, Quaternion motorRot, float tickDelta)
        {
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
                _targetPos = _smoothPos = correctedPos;
                Quaternion corrRot = Quaternion.Euler(0f, correctedRotY, 0f);
                _targetRot = _smoothRot = corrRot;
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
                    $"smoothTime={_interpSmoothTime:F3}s " +
                    $"mode=ExponentialSmoothing");
            }

            _diagFrameCount++;
            float dt = Time.deltaTime;

            // 1. Exponential Smoothing: sanftes Annähern ans Target.
            //    alpha = 1 - exp(-dt / smoothTime):
            //      - dt klein (schneller Frame) → alpha klein → wenig Bewegung
            //      - dt gross (langsamer Frame) → alpha gross → mehr Bewegung
            //      - Frame-rate-unabhaengig: gleiche visuelle Geschwindigkeit bei 30 oder 120 fps
            //    Ergebnis: gleichmaessige visuelle Geschwindigkeit proportional zur Distanz.
            //    Kein Stoppen (exponential approach erreicht Target nie exakt),
            //    kein Sprinten (kein diskretes Umschalten der Rate).
            float alpha = 1f - Mathf.Exp(-dt / _interpSmoothTime);
            _smoothPos = Vector3.Lerp(_smoothPos, _targetPos, alpha);
            _smoothRot = Quaternion.Slerp(_smoothRot, _targetRot, alpha);

            // 2. Offset-Decay (Reconcile Correction)
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

            // 3. Final Visual = Smooth Position + Correction Offset
            Vector3 finalPos = _smoothPos + _positionOffset;
            Quaternion finalRot = _smoothRot * Quaternion.Euler(0f, _rotationOffset, 0f);

            transform.SetPositionAndRotation(finalPos, finalRot);

            // --- Diagnostics ---
            if (_debugLog)
            {
                Vector3 delta = finalPos - _diagLastFinalPos;
                float distToTarget = Vector3.Distance(_smoothPos, _targetPos);

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
                            $"dt={dt:F4}s distToTarget={distToTarget:F3}m alpha={alpha:F3} " +
                            $"frame={_diagFrameCount}");
                    }
                }

                // Status alle 120 Frames
                if (_diagFrameCount % 120 == 0)
                {
                    Debug.Log($"[Smoother] Status frame={_diagFrameCount}: " +
                        $"pos={finalPos:F3} smooth={_smoothPos:F3} target={_targetPos:F3} " +
                        $"distToTarget={distToTarget:F3}m smoothTime={_interpSmoothTime:F3}s " +
                        $"offset={_positionOffset:F4} " +
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
