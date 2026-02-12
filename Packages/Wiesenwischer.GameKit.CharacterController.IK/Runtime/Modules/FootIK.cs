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
        [SerializeField] private float _footOffset = 0.02f;

        [Tooltip("SmoothDamp-Zeit für Body-Offset.")]
        [SerializeField] private float _bodyOffsetSmooth = 0.1f;

        [Tooltip("Maximaler Fuß-Versatz (verhindert Überdehnung).")]
        [SerializeField] private float _maxFootAdjustment = 0.4f;

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

            _leftFootHit = CastFoot(leftFoot, out _leftFootTarget, out _leftFootRotation);
            _rightFootHit = CastFoot(rightFoot, out _rightFootTarget, out _rightFootRotation);

            // Body Offset berechnen
            float targetBodyOffset = 0f;
            if (_leftFootHit && _rightFootHit)
            {
                float leftDelta = _leftFootTarget.y - leftFoot.y;
                float rightDelta = _rightFootTarget.y - rightFoot.y;
                targetBodyOffset = Mathf.Min(leftDelta, rightDelta);
                targetBodyOffset = Mathf.Min(targetBodyOffset, 0f); // Nur nach unten
            }

            _currentBodyOffset = Mathf.SmoothDamp(
                _currentBodyOffset, targetBodyOffset, ref _bodyOffsetVelocity, _bodyOffsetSmooth);
        }

        public void ProcessIK(Animator animator, int layerIndex)
        {
            if (layerIndex != 0) return;

            float effectiveWeight = _weight;

            // Body Offset anwenden (Hüfte absenken)
            if (Mathf.Abs(_currentBodyOffset) > 0.001f)
            {
                Vector3 bodyPos = animator.bodyPosition;
                bodyPos.y += _currentBodyOffset;
                animator.bodyPosition = bodyPos;
            }

            // Left Foot
            if (_leftFootHit)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, effectiveWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, effectiveWeight);
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
                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, effectiveWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, effectiveWeight);
                animator.SetIKPosition(AvatarIKGoal.RightFoot, ClampFootTarget(_rightFootTarget, animator, AvatarIKGoal.RightFoot));
                animator.SetIKRotation(AvatarIKGoal.RightFoot, _rightFootRotation);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
            }
        }

        private bool CastFoot(Vector3 footPos, out Vector3 targetPos, out Quaternion targetRot)
        {
            Vector3 origin = footPos + Vector3.up * _raycastHeight;
            float distance = _raycastHeight + _raycastDepth;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, _groundLayers))
            {
                targetPos = hit.point + Vector3.up * _footOffset;
                targetRot = Quaternion.FromToRotation(Vector3.up, hit.normal)
                            * transform.rotation;
                return true;
            }

            targetPos = footPos;
            targetRot = Quaternion.identity;
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
