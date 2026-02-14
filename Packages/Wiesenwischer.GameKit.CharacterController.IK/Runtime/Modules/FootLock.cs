using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.CharacterController.IK.Modules
{
    /// <summary>
    /// Foot Locking (Anti-Sliding): Nagelt Füße per Velocity-Erkennung an ihrer
    /// Welt-Position fest, solange sie am Boden stehen. Verhindert sichtbares
    /// Foot Sliding bei Animations-Übergängen (Walk→Idle, Run→Stop).
    /// Registriert sich VOR FootIK beim IKManager.
    /// </summary>
    public class FootLock : MonoBehaviour, IIKModule
    {
        [Header("References")]
        [SerializeField] private PlayerController _playerController;

        [Header("Detection")]
        [Tooltip("Geschwindigkeit (m/s) unter der ein Fuß als stehend gilt.")]
        [SerializeField] private float _lockVelocityThreshold = 0.05f;

        [Tooltip("Geschwindigkeit (m/s) über der ein Lock gelöst wird.")]
        [SerializeField] private float _releaseVelocityThreshold = 0.15f;

        [Tooltip("Frames unter Lock-Threshold bevor Lock greift.")]
        [SerializeField] private int _stableFramesRequired = 2;

        [Header("Release")]
        [Tooltip("Smooth Blend-Out Zeit beim Lösen (Sekunden).")]
        [SerializeField] private float _releaseDuration = 0.15f;

        [Tooltip("Maximaler Abstand (m) zwischen gelockter und animierter Position. Darüber wird Lock gelöst.")]
        [SerializeField] private float _maxLockDistance = 0.3f;

        [Header("IK Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float _weight = 1f;

        private bool _isEnabled = true;
        private IKManager _ikManager;
        private Transform _rootTransform;

        private FootState _leftFoot;
        private FootState _rightFoot;

        // IIKModule
        public string Name => "FootLock";
        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }
        public float Weight { get => _weight; set => _weight = Mathf.Clamp01(value); }

        /// <summary>
        /// Ob der linke Fuß gerade gelockt ist (inkl. Release-Blend).
        /// FootIK prüft dieses Flag und überspringt gelockte Füße.
        /// </summary>
        public bool IsLeftFootLocked => _leftFoot.IsLocked || _leftFoot.IsReleasing;

        /// <summary>
        /// Ob der rechte Fuß gerade gelockt ist (inkl. Release-Blend).
        /// FootIK prüft dieses Flag und überspringt gelockte Füße.
        /// </summary>
        public bool IsRightFootLocked => _rightFoot.IsLocked || _rightFoot.IsReleasing;

        private void Awake()
        {
            _ikManager = GetComponent<IKManager>();
            if (_playerController == null)
                _playerController = GetComponentInParent<PlayerController>();
            _rootTransform = _playerController != null ? _playerController.transform : transform.parent;
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
            var animator = GetComponent<Animator>();
            if (animator == null || !animator.isHuman) return;

            var leftFootBone = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightFootBone = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (leftFootBone == null || rightFootBone == null) return;

            bool isGrounded = _playerController != null && _playerController.IsGrounded;

            if (!isGrounded)
            {
                ReleaseFoot(ref _leftFoot);
                ReleaseFoot(ref _rightFoot);
                return;
            }

            _leftFoot = CalculateFootLock(
                leftFootBone.position, leftFootBone.rotation,
                _rootTransform.position, _rootTransform.rotation,
                _leftFoot, isGrounded, Time.deltaTime,
                _lockVelocityThreshold, _releaseVelocityThreshold,
                _stableFramesRequired, _releaseDuration, _maxLockDistance);

            _rightFoot = CalculateFootLock(
                rightFootBone.position, rightFootBone.rotation,
                _rootTransform.position, _rootTransform.rotation,
                _rightFoot, isGrounded, Time.deltaTime,
                _lockVelocityThreshold, _releaseVelocityThreshold,
                _stableFramesRequired, _releaseDuration, _maxLockDistance);
        }

        public void ProcessIK(Animator animator, int layerIndex)
        {
            if (layerIndex != 0) return;

            ApplyFootLock(animator, AvatarIKGoal.LeftFoot, _leftFoot);
            ApplyFootLock(animator, AvatarIKGoal.RightFoot, _rightFoot);
        }

        private void ApplyFootLock(Animator animator, AvatarIKGoal goal, FootState state)
        {
            if (!state.IsLocked && !state.IsReleasing) return;

            float weight = _weight;
            if (state.IsReleasing)
                weight *= 1f - Mathf.Clamp01(state.ReleaseTimer / _releaseDuration);

            Vector3 worldPos = _rootTransform.TransformPoint(state.LockedLocalPos);
            Quaternion worldRot = _rootTransform.rotation * state.LockedLocalRot;

            animator.SetIKPositionWeight(goal, weight);
            animator.SetIKRotationWeight(goal, weight);
            animator.SetIKPosition(goal, worldPos);
            animator.SetIKRotation(goal, worldRot);
        }

        private static void ReleaseFoot(ref FootState state)
        {
            if (state.IsLocked)
            {
                state.IsLocked = false;
                state.IsReleasing = true;
                state.ReleaseTimer = 0f;
            }
            state.StableCount = 0;
        }

        internal static FootState CalculateFootLock(
            Vector3 footWorldPos,
            Quaternion footWorldRot,
            Vector3 rootPosition,
            Quaternion rootRotation,
            FootState state,
            bool isGrounded,
            float deltaTime,
            float lockThreshold,
            float releaseThreshold,
            int stableFramesRequired,
            float releaseDuration,
            float maxLockDistance)
        {
            float velocity = deltaTime > 0f
                ? (footWorldPos - state.PrevWorldPos).magnitude / deltaTime
                : 0f;
            state.PrevWorldPos = footWorldPos;

            if (state.IsLocked)
            {
                // Transform locked local position to world for distance check
                Vector3 lockedWorldPos = rootPosition + rootRotation * state.LockedLocalPos;
                if ((footWorldPos - lockedWorldPos).magnitude > maxLockDistance)
                {
                    state.IsLocked = false;
                    state.IsReleasing = true;
                    state.ReleaseTimer = 0f;
                    return state;
                }

                if (velocity > releaseThreshold)
                {
                    state.IsLocked = false;
                    state.IsReleasing = true;
                    state.ReleaseTimer = 0f;
                }
            }
            else if (!state.IsReleasing)
            {
                if (velocity < lockThreshold && isGrounded)
                {
                    state.StableCount++;
                    if (state.StableCount >= stableFramesRequired)
                    {
                        // Store in root-local space
                        Quaternion invRootRot = Quaternion.Inverse(rootRotation);
                        state.LockedLocalPos = invRootRot * (footWorldPos - rootPosition);
                        state.LockedLocalRot = invRootRot * footWorldRot;
                        state.IsLocked = true;
                        state.StableCount = 0;
                    }
                }
                else
                {
                    state.StableCount = 0;
                }
            }

            if (state.IsReleasing)
            {
                state.ReleaseTimer += deltaTime;
                if (state.ReleaseTimer >= releaseDuration)
                    state.IsReleasing = false;
            }

            return state;
        }

        internal struct FootState
        {
            public bool IsLocked;
            public bool IsReleasing;
            public Vector3 LockedLocalPos;
            public Quaternion LockedLocalRot;
            public Vector3 PrevWorldPos;
            public int StableCount;
            public float ReleaseTimer;
        }
    }
}
