using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.CharacterController.IK.Modules
{
    /// <summary>
    /// Foot IK Modul: Passt Fuß-Positionen an das Terrain an.
    /// Raycasts pro Fuß ermitteln die Bodenkontaktpunkte,
    /// IK verschiebt die Füße auf den Boden und rotiert die Fußsohlen
    /// entlang der Oberflächen-Normalen.
    /// </summary>
    public class FootIK : MonoBehaviour, IIKModule
    {
        [Header("References")]
        [SerializeField] private PlayerController _playerController;

        [Header("IK Settings")]
        [Tooltip("IK Weight (0=aus, 1=voll).")]
        [Range(0f, 1f)]
        [SerializeField] private float _weight = 1f;

        [Header("Raycast")]
        [Tooltip("Raycast-Start über dem Fuß.")]
        [SerializeField] private float _raycastHeight = 0.5f;

        [Tooltip("Raycast-Länge unter dem Fuß.")]
        [SerializeField] private float _raycastDepth = 0.3f;

        [Tooltip("Welche Layer als Boden gelten.")]
        [SerializeField] private LayerMask _groundLayers = ~0;

        [Header("Adjustments")]
        [Tooltip("Abstand Fußsohle über dem Boden (verhindert Clipping).")]
        [SerializeField] private float _footOffset = 0.04f;

        [Tooltip("SmoothDamp-Zeit für Body-Offset.")]
        [SerializeField] private float _bodyOffsetSmooth = 0.1f;

        [Tooltip("Maximaler Fuß-Versatz (verhindert Überdehnung).")]
        [SerializeField] private float _maxFootAdjustment = 0.4f;

        [Tooltip("Maximaler Body-Offset nach oben (kompensiert footOffset).")]
        [SerializeField] private float _maxBodyUpOffset = 0.05f;

        [Header("Terrain Adaptation")]
        [Tooltip("Höhendifferenz (m) ab der IK voll aktiv wird.")]
        [SerializeField] private float _terrainVarianceThreshold = 0.03f;

        [Tooltip("Minimaler Fuß-Versatz (m) ab dem IK eingreift.")]
        [SerializeField] private float _footDeadZone = 0.02f;

        [Header("Locomotion Blend")]
        [Tooltip("Ab dieser Geschwindigkeit wird FootIK ausgeblendet (m/s).")]
        [SerializeField] private float _speedBlendStart = 0.1f;

        [Tooltip("Bei dieser Geschwindigkeit ist FootIK komplett aus (m/s).")]
        [SerializeField] private float _speedBlendEnd = 0.5f;

        [Tooltip("SmoothDamp-Zeit für IK-Weight-Blending.")]
        [SerializeField] private float _weightBlendSmooth = 0.15f;

        private bool _isEnabled = true;
        private IKManager _ikManager;

        // Raycast-Ergebnisse (berechnet in PrepareIK)
        private bool _leftFootHit;
        private bool _rightFootHit;
        private Vector3 _leftFootTarget;
        private Vector3 _rightFootTarget;
        private Quaternion _leftFootRotation;
        private Quaternion _rightFootRotation;

        // Body Offset
        private float _currentBodyOffset;
        private float _bodyOffsetVelocity;

        // Terrain-Varianz
        private float _terrainWeight = 1f;
        private Vector3 _leftFootNormal;
        private Vector3 _rightFootNormal;

        // Locomotion Blend: IK-Weight wird bei Bewegung ausgeblendet,
        // damit die Walk/Run-Animation die Beine steuert (nicht IK).
        private float _locomotionBlendWeight = 1f;
        private float _locomotionBlendVelocity;

        // IIKModule
        public string Name => "FootIK";
        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }
        public float Weight { get => _weight; set => _weight = Mathf.Clamp01(value); }

        private void Awake()
        {
            _ikManager = GetComponent<IKManager>();
            if (_playerController == null)
                _playerController = GetComponentInParent<PlayerController>();
        }

        private void OnEnable()
        {
            _ikManager?.RegisterModule(this);
        }

        private void OnDisable()
        {
            _ikManager?.UnregisterModule(this);
        }

        public void PrepareIK()
        {
            // Locomotion Blend: Bei Bewegung IK ausblenden, damit Walk/Run-Animation
            // die Beine steuert. Bei Idle volles IK für Terrain-Anpassung.
            float horizontalSpeed = _playerController.ReusableData?.HorizontalVelocity.magnitude ?? 0f;
            float targetBlend = 1f;
            if (horizontalSpeed > _speedBlendEnd)
                targetBlend = 0f;
            else if (horizontalSpeed > _speedBlendStart)
                targetBlend = 1f - Mathf.InverseLerp(_speedBlendStart, _speedBlendEnd, horizontalSpeed);

            _locomotionBlendWeight = Mathf.SmoothDamp(
                _locomotionBlendWeight, targetBlend, ref _locomotionBlendVelocity, _weightBlendSmooth);

            if (!_playerController.IsGrounded)
            {
                _leftFootHit = false;
                _rightFootHit = false;
                _currentBodyOffset = Mathf.SmoothDamp(
                    _currentBodyOffset, 0f, ref _bodyOffsetVelocity, _bodyOffsetSmooth);
                return;
            }

            var animator = GetComponent<Animator>();
            if (animator == null || !animator.isHuman || animator.avatar == null)
                return;

            var leftFootBone = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightFootBone = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (leftFootBone == null || rightFootBone == null)
                return;

            Vector3 leftFoot = leftFootBone.position;
            Vector3 rightFoot = rightFootBone.position;

            _leftFootHit = CastFoot(leftFoot, out _leftFootTarget, out _leftFootRotation, out _leftFootNormal);
            _rightFootHit = CastFoot(rightFoot, out _rightFootTarget, out _rightFootRotation, out _rightFootNormal);

            // Terrain-Varianz berechnen
            float terrainVariance = 0f;
            if (_leftFootHit && _rightFootHit)
            {
                float heightDiff = Mathf.Abs(_leftFootTarget.y - _rightFootTarget.y);
                float leftNormalDev = 1f - Vector3.Dot(_leftFootNormal, Vector3.up);
                float rightNormalDev = 1f - Vector3.Dot(_rightFootNormal, Vector3.up);
                float normalDev = Mathf.Max(leftNormalDev, rightNormalDev);
                terrainVariance = heightDiff + normalDev * 0.1f;
            }
            _terrainWeight = Mathf.InverseLerp(0f, _terrainVarianceThreshold, terrainVariance);

            // Body Offset berechnen (auch mit Locomotion Blend skaliert)
            float targetBodyOffset = 0f;
            if (_leftFootHit && _rightFootHit)
            {
                float leftDelta = _leftFootTarget.y - leftFoot.y;
                float rightDelta = _rightFootTarget.y - rightFoot.y;
                targetBodyOffset = Mathf.Min(leftDelta, rightDelta);
                targetBodyOffset = Mathf.Clamp(targetBodyOffset, -_maxFootAdjustment, _maxBodyUpOffset);
            }

            _currentBodyOffset = Mathf.SmoothDamp(
                _currentBodyOffset, targetBodyOffset, ref _bodyOffsetVelocity, _bodyOffsetSmooth);
        }

        public void ProcessIK(Animator animator, int layerIndex)
        {
            if (layerIndex != 0) return;

            // IK-Weight × Locomotion-Blend × Terrain-Varianz
            float effectiveWeight = _weight * _locomotionBlendWeight * _terrainWeight;

            // Body Offset anwenden (Hüfte absenken), auch mit Blend skaliert
            if (Mathf.Abs(_currentBodyOffset * _locomotionBlendWeight) > 0.001f)
            {
                Vector3 bodyPos = animator.bodyPosition;
                bodyPos.y += _currentBodyOffset * _locomotionBlendWeight;
                animator.bodyPosition = bodyPos;
            }

            // Left Foot
            if (_leftFootHit)
            {
                float leftDelta = (_leftFootTarget - animator.GetIKPosition(AvatarIKGoal.LeftFoot)).magnitude;
                float leftFootWeight = effectiveWeight * Mathf.InverseLerp(0f, _footDeadZone, leftDelta);

                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
                animator.SetIKPosition(AvatarIKGoal.LeftFoot, ClampFootTarget(_leftFootTarget, animator, AvatarIKGoal.LeftFoot));
                animator.SetIKRotation(AvatarIKGoal.LeftFoot, _leftFootRotation);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
            }

            // Right Foot
            if (_rightFootHit)
            {
                float rightDelta = (_rightFootTarget - animator.GetIKPosition(AvatarIKGoal.RightFoot)).magnitude;
                float rightFootWeight = effectiveWeight * Mathf.InverseLerp(0f, _footDeadZone, rightDelta);

                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootWeight);
                animator.SetIKPosition(AvatarIKGoal.RightFoot, ClampFootTarget(_rightFootTarget, animator, AvatarIKGoal.RightFoot));
                animator.SetIKRotation(AvatarIKGoal.RightFoot, _rightFootRotation);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
            }
        }

        private bool CastFoot(Vector3 footPos, out Vector3 targetPos, out Quaternion targetRot,
                              out Vector3 surfaceNormal)
        {
            Vector3 origin = footPos + Vector3.up * _raycastHeight;
            float distance = _raycastHeight + _raycastDepth;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, _groundLayers))
            {
                targetPos = hit.point + hit.normal * _footOffset;
                targetRot = Quaternion.FromToRotation(Vector3.up, hit.normal)
                            * transform.rotation;
                surfaceNormal = hit.normal;
                return true;
            }

            targetPos = footPos;
            targetRot = Quaternion.identity;
            surfaceNormal = Vector3.up;
            return false;
        }

        private Vector3 ClampFootTarget(Vector3 target, Animator animator, AvatarIKGoal goal)
        {
            Vector3 currentPos = animator.GetIKPosition(goal);
            Vector3 delta = target - currentPos;
            if (delta.magnitude > _maxFootAdjustment)
                target = currentPos + delta.normalized * _maxFootAdjustment;
            return target;
        }
    }
}
