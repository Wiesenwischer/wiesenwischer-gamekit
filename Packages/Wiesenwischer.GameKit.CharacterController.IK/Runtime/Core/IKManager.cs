using System.Collections.Generic;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.CharacterController.IK
{
    /// <summary>
    /// Orchestriert IK-Module. Ruft registrierte Module im OnAnimatorIK-Callback
    /// auf und ermöglicht globale Steuerung (Master-Weight, Enable/Disable).
    /// Muss auf dem selben GameObject wie der Animator liegen.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class IKManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController _playerController;

        [Header("Global Settings")]
        [Tooltip("Master-Weight für alle IK-Module. 0 = alles aus, 1 = voller Einfluss.")]
        [Range(0f, 1f)]
        [SerializeField] private float _masterWeight = 1f;

        [Tooltip("IK deaktivieren während bestimmter Zustände (Jump, Fall, Land).")]
        [SerializeField] private bool _disableDuringAirborne = true;

        private Animator _animator;
        private readonly List<IIKModule> _modules = new List<IIKModule>();
        private bool _isValid;

        public float MasterWeight
        {
            get => _masterWeight;
            set => _masterWeight = Mathf.Clamp01(value);
        }

        public IReadOnlyList<IIKModule> Modules => _modules;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_playerController == null)
                _playerController = GetComponentInParent<PlayerController>();
        }

        private void OnEnable()
        {
            _isValid = _animator != null && _playerController != null;
        }

        public void RegisterModule(IIKModule module)
        {
            if (module != null && !_modules.Contains(module))
                _modules.Add(module);
        }

        public void UnregisterModule(IIKModule module)
        {
            _modules.Remove(module);
        }

        public T GetModule<T>() where T : class, IIKModule
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i] is T typed) return typed;
            }
            return null;
        }

        private void LateUpdate()
        {
            if (!_isValid) return;
            if (GetEffectiveWeight() <= 0f) return;

            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i].IsEnabled)
                    _modules[i].PrepareIK();
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!_isValid) return;

            float effectiveWeight = GetEffectiveWeight();
            if (effectiveWeight <= 0f) return;

            for (int i = 0; i < _modules.Count; i++)
            {
                var module = _modules[i];
                if (module.IsEnabled && module.Weight > 0f)
                {
                    module.ProcessIK(_animator, layerIndex);
                }
            }
        }

        private float GetEffectiveWeight()
        {
            if (_masterWeight <= 0f) return 0f;

            if (_disableDuringAirborne && !_playerController.IsGrounded)
                return 0f;

            return _masterWeight;
        }
    }
}
