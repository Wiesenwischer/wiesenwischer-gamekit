using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.CharacterController.IK.Modules
{
    /// <summary>
    /// Foot Locking (Anti-Sliding): Nagelt Füße an ihrer IK-Position fest,
    /// sobald der Character stillsteht. Verhindert sichtbares Foot Sliding
    /// bei Animations-Übergängen (Walk→Idle, Run→Stop).
    ///
    /// Nutzt die CHARACTER-Geschwindigkeit (nicht Fuß-Velocity) als Lock-Trigger.
    /// Grund: Während eines Animation-Crossfade bewegen sich die Fuß-IK-Positionen
    /// durch den Blend selbst — per-Fuß-Velocity erkennt den Stillstand zu spät.
    /// Die Character-Geschwindigkeit geht SOFORT auf 0 wenn der Input stoppt.
    ///
    /// Arbeitet mit IK-Positionen (animator.GetIKPosition), nicht Bone-Positionen.
    /// </summary>
    public class FootLock : MonoBehaviour, IIKModule
    {
        [Header("References")]
        [SerializeField] private PlayerController _playerController;

        [Header("Detection (Character Speed)")]
        [Tooltip("Character-Geschwindigkeit (m/s) unter der Füße gelockt werden.")]
        [SerializeField] private float _lockSpeedThreshold = 0.1f;

        [Tooltip("Character-Geschwindigkeit (m/s) über der Locks gelöst werden.")]
        [SerializeField] private float _releaseSpeedThreshold = 0.3f;

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

        // Cached in PrepareIK, used in ProcessIK
        private bool _isGrounded;
        private float _characterSpeed;

        private FootState _leftFoot;
        private FootState _rightFoot;

        // IIKModule
        public string Name => "FootLock";
        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }
        public float Weight { get => _weight; set => _weight = Mathf.Clamp01(value); }

        public bool IsLeftFootLocked => _leftFoot.IsLocked || _leftFoot.IsReleasing;
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
            _isGrounded = _playerController != null && _playerController.IsGrounded;
            _characterSpeed = _playerController?.Locomotion?.HorizontalVelocity.magnitude ?? 0f;

            if (!_isGrounded)
            {
                ReleaseFoot(ref _leftFoot);
                ReleaseFoot(ref _rightFoot);
            }
        }

        public void ProcessIK(Animator animator, int layerIndex)
        {
            if (layerIndex != 0) return;

            bool shouldLock = _isGrounded && _characterSpeed < _lockSpeedThreshold;
            bool shouldRelease = _characterSpeed > _releaseSpeedThreshold;

            // Fuß-Positionen: FootIK Terrain-Raycast bevorzugen (Fuß auf dem Boden),
            // Fallback auf Animation-IK-Position (flacher Boden ohne Terrain-Anpassung).
            var footIK = _ikManager.GetModule<FootIK>();

            Vector3 leftPos, rightPos;
            Quaternion leftRot, rightRot;

            if (footIK != null && footIK.LeftFootHit)
            {
                leftPos = footIK.LeftFootTarget;
                leftRot = footIK.LeftFootRotation;
            }
            else
            {
                leftPos = animator.GetIKPosition(AvatarIKGoal.LeftFoot);
                leftRot = animator.GetIKRotation(AvatarIKGoal.LeftFoot);
            }

            if (footIK != null && footIK.RightFootHit)
            {
                rightPos = footIK.RightFootTarget;
                rightRot = footIK.RightFootRotation;
            }
            else
            {
                rightPos = animator.GetIKPosition(AvatarIKGoal.RightFoot);
                rightRot = animator.GetIKRotation(AvatarIKGoal.RightFoot);
            }

            _leftFoot = CalculateFootLock(
                leftPos, leftRot,
                _rootTransform.position, _rootTransform.rotation,
                _leftFoot, shouldLock, shouldRelease, Time.deltaTime,
                _releaseDuration, _maxLockDistance);

            _rightFoot = CalculateFootLock(
                rightPos, rightRot,
                _rootTransform.position, _rootTransform.rotation,
                _rightFoot, shouldLock, shouldRelease, Time.deltaTime,
                _releaseDuration, _maxLockDistance);

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
        }

        internal static FootState CalculateFootLock(
            Vector3 footIKWorldPos,
            Quaternion footIKWorldRot,
            Vector3 rootPosition,
            Quaternion rootRotation,
            FootState state,
            bool shouldLock,
            bool shouldRelease,
            float deltaTime,
            float releaseDuration,
            float maxLockDistance)
        {
            if (state.IsLocked)
            {
                // Safety: Release if foot drifts too far from locked position
                Vector3 lockedWorldPos = rootPosition + rootRotation * state.LockedLocalPos;
                if ((footIKWorldPos - lockedWorldPos).magnitude > maxLockDistance)
                {
                    state.IsLocked = false;
                    state.IsReleasing = true;
                    state.ReleaseTimer = 0f;
                    return state;
                }

                // Release when character starts moving
                if (shouldRelease)
                {
                    state.IsLocked = false;
                    state.IsReleasing = true;
                    state.ReleaseTimer = 0f;
                }
            }
            else if (!state.IsReleasing)
            {
                // Lock when character is stationary
                if (shouldLock)
                {
                    Quaternion invRootRot = Quaternion.Inverse(rootRotation);
                    state.LockedLocalPos = invRootRot * (footIKWorldPos - rootPosition);
                    state.LockedLocalRot = invRootRot * footIKWorldRot;
                    state.IsLocked = true;
                }
            }

            // Release blend-out timer
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
            public float ReleaseTimer;
        }
    }
}
