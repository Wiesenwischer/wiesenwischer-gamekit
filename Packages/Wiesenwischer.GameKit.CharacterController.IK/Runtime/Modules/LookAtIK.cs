using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.IK.Modules
{
    /// <summary>
    /// LookAt IK Modul: Dreht Kopf und Oberkörper zum Blickziel.
    /// Nutzt Unity's eingebaute SetLookAtPosition/Weight API.
    /// Das Target kommt von einem IIKTargetProvider (z.B. CameraTargetProvider).
    /// </summary>
    public class LookAtIK : MonoBehaviour, IIKModule
    {
        [Header("Target")]
        [Tooltip("Referenz auf den IIKTargetProvider (muss MonoBehaviour sein).")]
        [SerializeField] private MonoBehaviour _targetProvider;

        [Header("IK Settings")]
        [Tooltip("Gesamt-IK-Weight.")]
        [Range(0f, 1f)]
        [SerializeField] private float _weight = 1f;

        [Tooltip("Wie stark sich der Oberkörper mitdreht (niedrig halten!).")]
        [Range(0f, 1f)]
        [SerializeField] private float _bodyWeight = 0.05f;

        [Tooltip("Kopf-Rotations-Stärke.")]
        [Range(0f, 1f)]
        [SerializeField] private float _headWeight = 0.5f;

        [Tooltip("Augen-Rotation (deaktiviert für CC-Modelle).")]
        [Range(0f, 1f)]
        [SerializeField] private float _eyesWeight = 0f;

        [Tooltip("Begrenzt extreme Drehwinkel (höher = mehr Clamping).")]
        [Range(0f, 1f)]
        [SerializeField] private float _clampWeight = 0.5f;

        [Tooltip("Wie schnell der LookAt-Weight sich ändert.")]
        [SerializeField] private float _smoothSpeed = 5f;

        private bool _isEnabled = true;
        private IKManager _ikManager;
        private IIKTargetProvider _target;
        private float _currentWeight;
        private Vector3 _currentLookTarget;

        // IIKModule
        public string Name => "LookAtIK";
        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }
        public float Weight { get => _weight; set => _weight = Mathf.Clamp01(value); }

        private void Awake()
        {
            _ikManager = GetComponent<IKManager>();
            _target = _targetProvider as IIKTargetProvider;

            if (_target == null)
                _target = GetComponent<IIKTargetProvider>();
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
            float targetWeight = (_target != null && _target.HasLookTarget) ? _weight : 0f;
            _currentWeight = Mathf.MoveTowards(_currentWeight, targetWeight,
                _smoothSpeed * Time.deltaTime);

            if (_target != null && _target.HasLookTarget)
            {
                Vector3 newTarget = _target.GetLookTarget();
                _currentLookTarget = Vector3.Lerp(_currentLookTarget, newTarget,
                    _smoothSpeed * Time.deltaTime);
            }
        }

        public void ProcessIK(Animator animator, int layerIndex)
        {
            if (layerIndex != 0) return;

            if (_currentWeight <= 0.01f)
            {
                animator.SetLookAtWeight(0f);
                return;
            }

            animator.SetLookAtPosition(_currentLookTarget);
            animator.SetLookAtWeight(
                _currentWeight,
                _bodyWeight,
                _headWeight,
                _eyesWeight,
                _clampWeight);
        }
    }
}
