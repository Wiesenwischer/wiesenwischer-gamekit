using System;
using System.Collections.Generic;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.Abilities.Core
{
    /// <summary>
    /// Central ability lifecycle manager.
    /// Handles registration, activation, deactivation, cooldowns and priority.
    /// Placed on the Player root alongside PlayerController.
    /// </summary>
    public class AbilitySystem : MonoBehaviour, IAbilitySystem
    {
        // === Events ===
        public event Action<IAbility> OnAbilityActivated;
        public event Action<IAbility> OnAbilityDeactivated;
        public event Action<IAbility> OnAbilityCancelled;
        public event Action<IAbility> OnAbilityCooldownComplete;

        // === Internal State ===
        private readonly Dictionary<string, AbilitySlot> _slots = new();
        private AbilitySlot _activeSlot;
        private AbilityContext _context;

        // Cached references
        private PlayerController _player;
        private CharacterMotor _motor;

        // === Public Properties ===

        /// <summary>Whether any ability is currently active.</summary>
        public bool HasActiveAbility => _activeSlot != null;

        /// <summary>The currently active ability (null if none).</summary>
        public IAbility ActiveAbility => _activeSlot?.Ability;

        // === Lifecycle ===

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _motor = GetComponent<CharacterMotor>();

            if (_player == null)
                Debug.LogError("[AbilitySystem] PlayerController nicht gefunden!");
            if (_motor == null)
                Debug.LogError("[AbilitySystem] CharacterMotor nicht gefunden!");
        }

        /// <summary>
        /// Called by PlayerController in its Update loop.
        /// Updates cooldowns and ticks active ability.
        /// </summary>
        public void Tick(float deltaTime)
        {
            UpdateContext();
            UpdateCooldowns(deltaTime);
            TickActiveAbility(deltaTime);
        }

        // === Registration ===

        /// <summary>Register an ability with its definition. Returns false if ID already registered.</summary>
        public bool RegisterAbility(IAbility ability, AbilityDefinition definition)
        {
            if (ability == null || definition == null)
            {
                Debug.LogWarning("[AbilitySystem] RegisterAbility: ability oder definition ist null.");
                return false;
            }

            string id = ability.Id;
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[AbilitySystem] RegisterAbility: Ability hat keine Id.");
                return false;
            }

            if (_slots.ContainsKey(id))
            {
                Debug.LogWarning($"[AbilitySystem] Ability '{id}' ist bereits registriert.");
                return false;
            }

            _slots[id] = new AbilitySlot(ability, definition);
            return true;
        }

        /// <summary>Unregister an ability by ID. Deactivates if currently active.</summary>
        public bool UnregisterAbility(string abilityId)
        {
            if (!_slots.TryGetValue(abilityId, out var slot))
                return false;

            if (_activeSlot == slot)
                Cancel(abilityId);

            _slots.Remove(abilityId);
            return true;
        }

        // === Activation ===

        /// <summary>
        /// Try to activate an ability by ID.
        /// Checks: registered? not on cooldown? CanActivate? priority?
        /// If a lower-priority ability is active and interruptible, it gets cancelled first.
        /// </summary>
        public bool TryActivate(string abilityId)
        {
            if (!_slots.TryGetValue(abilityId, out var slot))
                return false;

            // Cooldown check
            if (slot.IsOnCooldown)
                return false;

            // CanActivate check
            UpdateContext();
            if (!slot.Ability.CanActivate(_context))
                return false;

            // Priority check against active ability
            if (_activeSlot != null)
            {
                // Same ability already active
                if (_activeSlot == slot)
                    return false;

                // New priority must be >= active priority to interrupt
                if (slot.Ability.Priority < _activeSlot.Ability.Priority)
                    return false;

                // Active ability must be interruptible
                if (!_activeSlot.Definition.interruptible)
                    return false;

                // Cancel active ability
                CancelInternal(_activeSlot);
            }

            // Activate
            slot.Ability.Activate(_context);
            _activeSlot = slot;
            HandleAbilityAnimation(slot.Ability, true);
            OnAbilityActivated?.Invoke(slot.Ability);
            return true;
        }

        /// <summary>Deactivate an ability by ID. Starts cooldown.</summary>
        public void Deactivate(string abilityId)
        {
            if (!_slots.TryGetValue(abilityId, out var slot))
                return;

            if (_activeSlot != slot)
                return;

            DeactivateInternal(slot);
        }

        /// <summary>Cancel an ability (interrupted by priority or external force).</summary>
        public void Cancel(string abilityId)
        {
            if (!_slots.TryGetValue(abilityId, out var slot))
                return;

            if (_activeSlot != slot)
                return;

            CancelInternal(slot);
        }

        // === Queries ===

        /// <summary>Get an ability by ID. Returns null if not registered.</summary>
        public IAbility GetAbility(string abilityId)
        {
            return _slots.TryGetValue(abilityId, out var slot) ? slot.Ability : null;
        }

        /// <summary>Remaining cooldown for an ability (0 if ready).</summary>
        public float GetCooldownRemaining(string abilityId)
        {
            return _slots.TryGetValue(abilityId, out var slot) ? slot.CooldownRemaining : 0f;
        }

        // === Internal ===

        private void UpdateContext()
        {
            _context = new AbilityContext(
                _player,
                _motor,
                _player != null ? _player.Locomotion : null,
                _player != null ? _player.AnimationController : null
            );
        }

        private void UpdateCooldowns(float deltaTime)
        {
            foreach (var kvp in _slots)
            {
                var slot = kvp.Value;
                if (!slot.IsOnCooldown) continue;

                slot.CooldownRemaining -= deltaTime;
                if (slot.CooldownRemaining <= 0f)
                {
                    slot.CooldownRemaining = 0f;
                    OnAbilityCooldownComplete?.Invoke(slot.Ability);
                }
            }
        }

        private void TickActiveAbility(float deltaTime)
        {
            if (_activeSlot == null) return;

            _activeSlot.Ability.Tick(_context, deltaTime);

            // Auto-deactivate when animation completes
            if (_activeSlot != null
                && _activeSlot.Ability is IAbilityAnimationHandler animHandler
                && animHandler.IsAnimationComplete(_context))
            {
                DeactivateInternal(_activeSlot);
            }
        }

        private void DeactivateInternal(AbilitySlot slot)
        {
            HandleAbilityAnimation(slot.Ability, false);
            slot.Ability.Deactivate(_context);
            StartCooldown(slot);
            _activeSlot = null;
            OnAbilityDeactivated?.Invoke(slot.Ability);
        }

        private void CancelInternal(AbilitySlot slot)
        {
            HandleAbilityAnimation(slot.Ability, false);
            slot.Ability.Deactivate(_context);
            StartCooldown(slot);
            _activeSlot = null;
            OnAbilityCancelled?.Invoke(slot.Ability);
        }

        private void HandleAbilityAnimation(IAbility ability, bool activating)
        {
            var animController = _player != null ? _player.AnimationController : null;
            if (animController == null) return;

            if (activating)
            {
                animController.SetAbilityLayerWeight(1f);

                if (ability is IAbilityAnimationHandler animHandler
                    && !string.IsNullOrEmpty(animHandler.AnimationStateName))
                {
                    animController.PlayAbilityAnimation(
                        animHandler.AnimationStateName,
                        animHandler.TransitionDuration);
                }
            }
            else
            {
                animController.SetAbilityLayerWeight(0f);
            }
        }

        private void StartCooldown(AbilitySlot slot)
        {
            if (slot.Definition.cooldown > 0f)
                slot.CooldownRemaining = slot.Definition.cooldown;
        }
    }
}
