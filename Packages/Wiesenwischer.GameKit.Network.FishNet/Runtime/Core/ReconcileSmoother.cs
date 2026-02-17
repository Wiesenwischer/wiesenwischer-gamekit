using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Visuelles Smoothing fuer Netzwerk-Reconciliation.
    /// Speichert einen Correction-Offset und baut ihn exponentiell ab.
    ///
    /// Laeuft in LateUpdate NACH CharacterMotorSystem (ExecOrder -100).
    /// Motor setzt Transform.position via CustomInterpolationUpdate().
    /// ReconcileSmoother addiert den abklingenden Offset darauf.
    ///
    /// Flow:
    /// 1. NetworkCharacterDriver berechnet Error nach Reconcile+Replay
    /// 2. SetCorrectionOffset(error) → Offset wird gespeichert
    /// 3. Jedes Frame: Offset decayed exponentiell → visuell sanfte Korrektur
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

        private Vector3 _positionOffset;
        private float _rotationOffset;

        /// <summary>Snap-Threshold fuer externe Abfrage (NetworkCharacterDriver).</summary>
        public float SnapThreshold => _snapThreshold;

        /// <summary>Aktueller visueller Offset (fuer Debug).</summary>
        public Vector3 CurrentOffset => _positionOffset;

        /// <summary>Aktueller Rotations-Offset in Grad (fuer Debug).</summary>
        public float CurrentRotationOffset => _rotationOffset;

        /// <summary>
        /// Setzt einen neuen Correction-Offset.
        /// Der Offset wird ueber mehrere Frames exponentiell abgebaut.
        /// </summary>
        public void SetCorrectionOffset(Vector3 positionError, float rotationError)
        {
            _positionOffset = positionError;
            _rotationOffset = rotationError;
        }

        /// <summary>
        /// Setzt den Offset sofort auf Zero (hard snap).
        /// </summary>
        public void ClearOffset()
        {
            _positionOffset = Vector3.zero;
            _rotationOffset = 0f;
        }

        private void LateUpdate()
        {
            bool hasPosition = _positionOffset.sqrMagnitude > _minCorrectionThreshold * _minCorrectionThreshold;
            bool hasRotation = Mathf.Abs(_rotationOffset) > _minCorrectionThreshold;

            if (!hasPosition && !hasRotation)
            {
                _positionOffset = Vector3.zero;
                _rotationOffset = 0f;
                return;
            }

            // Frame-rate-unabhaengiger exponentieller Decay.
            // Bei 60fps und rate=0.25: factor = (1-0.25)^1 = 0.75 → 25% Reduktion pro Frame.
            // Bei 30fps: factor = (1-0.25)^2 = 0.5625 → ~44% Reduktion pro Frame (= gleiche Rate ueber Zeit).
            float dt = Time.deltaTime * 60f; // Normalisiert auf 60fps
            float posFactor = Mathf.Pow(1f - _correctionRate, dt);
            float rotFactor = Mathf.Pow(1f - _rotationCorrectionRate, dt);

            _positionOffset *= posFactor;
            _rotationOffset *= rotFactor;

            // Micro-Jitter vermeiden
            if (_positionOffset.sqrMagnitude < _minCorrectionThreshold * _minCorrectionThreshold)
                _positionOffset = Vector3.zero;
            if (Mathf.Abs(_rotationOffset) < _minCorrectionThreshold)
                _rotationOffset = 0f;

            // Offset auf Transform anwenden (NACH Motor's CustomInterpolationUpdate)
            if (_positionOffset != Vector3.zero)
                transform.position += _positionOffset;

            if (_rotationOffset != 0f)
                transform.rotation *= Quaternion.Euler(0f, _rotationOffset, 0f);
        }
    }
}
