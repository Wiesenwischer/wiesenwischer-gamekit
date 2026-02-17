using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Einheitliches Visual-Smoothing fuer Netzwerk-Characters.
    /// Handhabt BEIDES: Tick-Interpolation UND Reconcile-Correction.
    ///
    /// Ersetzt die KCC-eigene CustomInterpolationUpdate (CharacterMotorSystem.Settings.Interpolate = false).
    /// Damit gibt es nur EIN System das Transform.position schreibt — kein Kaempfen.
    ///
    /// Flow:
    /// 1. OnPreTick(): Speichert Interpolations-Startpunkt (TransientPosition VOR Simulation)
    /// 2. [Replicate]: Simulation laeuft, TransientPosition aendert sich
    ///    → CharacterMotorSystem.Simulate() schreibt TransientPosition auf transform.position (fuer Collider-Queries)
    /// 3. OnReconcileComplete(): Bei Reconcile — Error berechnen, Startpunkt korrigieren
    /// 4. OnPostTick(): Speichert Interpolations-Endpunkt + STELLT VISUELLE POSITION WIEDER HER
    ///    → Verhindert dass Kamera/Animator/etc. die rohe Simulation-Position sehen
    /// 5. LateUpdate: Interpoliert zwischen Start/End + decaying Correction-Offset
    /// </summary>
    [DefaultExecutionOrder(100)]
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

        // --- Tick Interpolation ---
        private Vector3 _tickStartPos;
        private Quaternion _tickStartRot;
        private Vector3 _tickEndPos;
        private Quaternion _tickEndRot;
        private float _interpStartTime;
        private float _interpDeltaTime;
        private bool _initialized;

        // --- Correction Offset ---
        private Vector3 _positionOffset;
        private float _rotationOffset;

        // --- Letzte visuelle Position (fuer Restore nach Tick-Processing) ---
        private Vector3 _lastSetPosition;
        private Quaternion _lastSetRotation = Quaternion.identity;
        private int _diagFrameCount;

        /// <summary>Snap-Threshold fuer externe Abfrage.</summary>
        public float SnapThreshold => _snapThreshold;

        /// <summary>Aktueller visueller Offset (fuer Debug).</summary>
        public Vector3 CurrentOffset => _positionOffset;

        /// <summary>Aktueller Rotations-Offset in Grad (fuer Debug).</summary>
        public float CurrentRotationOffset => _rotationOffset;

        /// <summary>Ob der Smoother aktiv interpoliert (nach erstem OnPreTick).</summary>
        public bool IsActive => _initialized;

        #region Tick Lifecycle

        /// <summary>
        /// Wird von NetworkCharacterDriver VOR dem Tick aufgerufen.
        /// Speichert den Interpolations-Startpunkt (= aktuelle Motor-Position).
        /// </summary>
        public void OnPreTick(Vector3 motorPos, Quaternion motorRot)
        {
            _tickStartPos = motorPos;
            _tickStartRot = motorRot;

            if (!_initialized)
            {
                // Erste visuelle Position initialisieren (vor erstem LateUpdate)
                _lastSetPosition = transform.position;
                _lastSetRotation = transform.rotation;
            }

            _initialized = true;
        }

        /// <summary>
        /// Wird von NetworkCharacterDriver NACH dem Tick aufgerufen.
        /// Speichert den Interpolations-Endpunkt und startet das Timing.
        /// </summary>
        public void OnPostTick(Vector3 motorPos, Quaternion motorRot, float tickDelta)
        {
            _tickEndPos = motorPos;
            _tickEndRot = motorRot;
            _interpStartTime = Time.time;
            _interpDeltaTime = tickDelta;

            // KRITISCH: CharacterMotorSystem.Simulate() hat transform.position auf
            // TransientPosition gesetzt (noetig fuer Collider-Queries waehrend der Simulation).
            // Ohne Restore sehen Kamera, Animator und andere Systeme zwischen OnPostTick
            // und LateUpdate die rohe Simulation-Position statt der geglatteten → Jitter.
            // Equivalent zu KCC's PostSimulationInterpolationUpdate.
            if (_initialized)
                transform.SetPositionAndRotation(_lastSetPosition, _lastSetRotation);
        }

        #endregion

        #region Reconcile Correction

        /// <summary>
        /// Wird nach Owner-Reconcile+Replay aufgerufen (in PerformReplicate, ContainsTicked).
        /// Berechnet Error, akkumuliert Offset, und korrigiert den Interpolations-Startpunkt.
        /// </summary>
        public void OnReconcileComplete(Vector3 preReconcilePos, float preReconcileRotY,
                                         Vector3 correctedPos, float correctedRotY)
        {
            Vector3 posError = preReconcilePos - correctedPos;
            float rotError = Mathf.DeltaAngle(correctedRotY, preReconcileRotY);

            if (posError.sqrMagnitude > _snapThreshold * _snapThreshold)
            {
                ClearOffset();
            }
            else
            {
                _positionOffset += posError;
                _rotationOffset += rotError;
            }

            // Interpolations-Startpunkt auf korrigierte Position setzen.
            // Verhindert dass die Interpolation die Korrektur-Distanz durchlaeuft.
            _tickStartPos = correctedPos;
            _tickStartRot = Quaternion.Euler(0f, correctedRotY, 0f);

            if (_debugLog && posError.sqrMagnitude > _minCorrectionThreshold * _minCorrectionThreshold)
                Debug.Log($"[ReconcileSmoother] Reconcile: pos={posError.magnitude:F4}m rot={rotError:F2}°");
        }

        /// <summary>
        /// Wird nach Spectator-Correction aufgerufen (nach Simulation mit neuem autoritativem Input).
        /// </summary>
        public void OnSpectatorCorrection(Vector3 prePos, Vector3 postPos, Quaternion postRot)
        {
            Vector3 error = prePos - postPos;

            if (error.sqrMagnitude > _snapThreshold * _snapThreshold)
                ClearOffset();
            else
                _positionOffset += error;

            // Endpunkt aktualisieren (Simulation hat mit neuem Input eine andere Position erzeugt)
            _tickEndPos = postPos;
            _tickEndRot = postRot;

            if (_debugLog && error.sqrMagnitude > _minCorrectionThreshold * _minCorrectionThreshold)
                Debug.Log($"[ReconcileSmoother] Spectator: pos={error.magnitude:F4}m");
        }

        /// <summary>
        /// Setzt den Offset sofort auf Zero (hard snap).
        /// </summary>
        public void ClearOffset()
        {
            _positionOffset = Vector3.zero;
            _rotationOffset = 0f;
        }

        /// <summary>
        /// Setzt den Smoother komplett zurueck (z.B. bei Disconnect).
        /// </summary>
        public void Reset()
        {
            ClearOffset();
            _initialized = false;
        }

        #endregion

        #region Visual Update

        private void LateUpdate()
        {
            // Offline-Guard: Ohne OnPreTick-Call kein Smoothing
            // (KCC handhabt Interpolation selbst via CustomInterpolationUpdate)
            if (!_initialized) return;

            // --- DIAGNOSE: Erkennung ob etwas anderes Transform.position aendert ---
            if (_debugLog)
            {
                _diagFrameCount++;
                Vector3 currentPos = transform.position;
                float drift = (currentPos - _lastSetPosition).magnitude;
                if (drift > 0.0001f)
                {
                    Debug.LogWarning($"[Smoother DIAG] Frame {_diagFrameCount}: Transform.position EXTERN geaendert! " +
                        $"Erwartet={_lastSetPosition:F4} Ist={currentPos:F4} Drift={drift:F4}m");
                }
            }

            // 1. Tick-Interpolation (ersetzt CharacterMotorSystem.CustomInterpolationUpdate)
            float factor = (_interpDeltaTime > 0f)
                ? Mathf.Clamp01((Time.time - _interpStartTime) / _interpDeltaTime)
                : 1f;

            Vector3 interpPos = Vector3.Lerp(_tickStartPos, _tickEndPos, factor);
            Quaternion interpRot = Quaternion.Slerp(_tickStartRot, _tickEndRot, factor);

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

            transform.SetPositionAndRotation(finalPos, finalRot);
            _lastSetPosition = finalPos;
            _lastSetRotation = finalRot;

            // --- DIAGNOSE: Detailliertes Logging alle N Frames ---
            if (_debugLog && _diagFrameCount % 30 == 0)
            {
                Debug.Log($"[Smoother DIAG] Frame {_diagFrameCount}: " +
                    $"tickStart={_tickStartPos:F3} tickEnd={_tickEndPos:F3} " +
                    $"factor={factor:F3} interp={interpPos:F3} " +
                    $"offset={_positionOffset:F4} final={finalPos:F3}");
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
