using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Abilities.Core.Tests
{
    [TestFixture]
    public class AbilitySystemTests
    {
        private AbilitySystem _system;
        private GameObject _gameObject;
        private AbilityDefinition _defaultDef;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("TestPlayer");

            // AbilitySystem braucht PlayerController + CharacterMotor auf dem selben GO,
            // aber für isolierte Unit Tests testen wir nur die Ability-Logik.
            // PlayerController/Motor-Referenzen bleiben null — Context-Zugriff wird nicht getestet.
            _system = _gameObject.AddComponent<AbilitySystem>();

            _defaultDef = ScriptableObject.CreateInstance<AbilityDefinition>();
            _defaultDef.abilityId = "test";
            _defaultDef.cooldown = 1f;
            _defaultDef.priority = AbilityPriority.Attack;
            _defaultDef.interruptible = true;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
            Object.DestroyImmediate(_defaultDef);
        }

        private AbilityDefinition CreateDef(float cooldown = 1f, int priority = AbilityPriority.Attack, bool interruptible = true)
        {
            var def = ScriptableObject.CreateInstance<AbilityDefinition>();
            def.cooldown = cooldown;
            def.priority = priority;
            def.interruptible = interruptible;
            return def;
        }

        #region Registration

        [Test]
        public void Register_ReturnsTrue_ForNewAbility()
        {
            var ability = new MockAbility { Id = "attack" };
            Assert.IsTrue(_system.RegisterAbility(ability, _defaultDef));
        }

        [Test]
        public void Register_ReturnsFalse_ForDuplicateId()
        {
            var a1 = new MockAbility { Id = "attack" };
            var a2 = new MockAbility { Id = "attack" };
            _system.RegisterAbility(a1, _defaultDef);

            Assert.IsFalse(_system.RegisterAbility(a2, _defaultDef));
        }

        [Test]
        public void Register_ReturnsFalse_ForNullAbility()
        {
            Assert.IsFalse(_system.RegisterAbility(null, _defaultDef));
        }

        [Test]
        public void Register_ReturnsFalse_ForNullDefinition()
        {
            var ability = new MockAbility { Id = "attack" };
            Assert.IsFalse(_system.RegisterAbility(ability, null));
        }

        [Test]
        public void Unregister_RemovesAbility()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);

            Assert.IsTrue(_system.UnregisterAbility("attack"));
            Assert.IsNull(_system.GetAbility("attack"));
        }

        [Test]
        public void Unregister_ReturnsFalse_WhenNotRegistered()
        {
            Assert.IsFalse(_system.UnregisterAbility("nonexistent"));
        }

        [Test]
        public void Unregister_DeactivatesActive()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);
            _system.TryActivate("attack");

            _system.UnregisterAbility("attack");

            Assert.AreEqual(1, ability.DeactivateCallCount);
            Assert.IsFalse(_system.HasActiveAbility);
        }

        #endregion

        #region Activation

        [Test]
        public void TryActivate_CallsActivate_WhenReady()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);

            Assert.IsTrue(_system.TryActivate("attack"));
            Assert.AreEqual(1, ability.ActivateCallCount);
            Assert.AreEqual(AbilityState.Active, ability.State);
            Assert.IsTrue(_system.HasActiveAbility);
        }

        [Test]
        public void TryActivate_ReturnsFalse_WhenOnCooldown()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);
            _system.TryActivate("attack");
            _system.Deactivate("attack"); // Starts cooldown

            Assert.IsFalse(_system.TryActivate("attack"));
        }

        [Test]
        public void TryActivate_ReturnsFalse_WhenCanActivateFalse()
        {
            var ability = new MockAbility { Id = "attack", CanActivateResult = false };
            _system.RegisterAbility(ability, _defaultDef);

            Assert.IsFalse(_system.TryActivate("attack"));
            Assert.AreEqual(0, ability.ActivateCallCount);
        }

        [Test]
        public void TryActivate_ReturnsFalse_WhenNotRegistered()
        {
            Assert.IsFalse(_system.TryActivate("nonexistent"));
        }

        [Test]
        public void TryActivate_ReturnsFalse_WhenSameAbilityActive()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);
            _system.TryActivate("attack");

            Assert.IsFalse(_system.TryActivate("attack"));
            Assert.AreEqual(1, ability.ActivateCallCount); // Not called again
        }

        #endregion

        #region Priority

        [Test]
        public void TryActivate_InterruptsLowerPriority()
        {
            var low = new MockAbility { Id = "interact", Priority = AbilityPriority.Interaction };
            var high = new MockAbility { Id = "dodge", Priority = AbilityPriority.Dodge };
            var lowDef = CreateDef(priority: AbilityPriority.Interaction);
            var highDef = CreateDef(priority: AbilityPriority.Dodge);

            _system.RegisterAbility(low, lowDef);
            _system.RegisterAbility(high, highDef);

            _system.TryActivate("interact");
            Assert.IsTrue(_system.TryActivate("dodge"));

            Assert.AreEqual(1, low.DeactivateCallCount); // Interrupted
            Assert.AreEqual(1, high.ActivateCallCount);
            Assert.AreSame(high, _system.ActiveAbility);

            Object.DestroyImmediate(lowDef);
            Object.DestroyImmediate(highDef);
        }

        [Test]
        public void TryActivate_FailsAgainstHigherPriority()
        {
            var high = new MockAbility { Id = "dodge", Priority = AbilityPriority.Dodge };
            var low = new MockAbility { Id = "interact", Priority = AbilityPriority.Interaction };
            var highDef = CreateDef(priority: AbilityPriority.Dodge);
            var lowDef = CreateDef(priority: AbilityPriority.Interaction);

            _system.RegisterAbility(high, highDef);
            _system.RegisterAbility(low, lowDef);

            _system.TryActivate("dodge");
            Assert.IsFalse(_system.TryActivate("interact"));

            Assert.AreSame(high, _system.ActiveAbility);
            Assert.AreEqual(0, low.ActivateCallCount);

            Object.DestroyImmediate(highDef);
            Object.DestroyImmediate(lowDef);
        }

        [Test]
        public void TryActivate_FailsWhenActiveNotInterruptible()
        {
            var first = new MockAbility { Id = "ultimate", Priority = AbilityPriority.Attack };
            var second = new MockAbility { Id = "dodge", Priority = AbilityPriority.Dodge };
            var firstDef = CreateDef(interruptible: false);
            var secondDef = CreateDef(priority: AbilityPriority.Dodge);

            _system.RegisterAbility(first, firstDef);
            _system.RegisterAbility(second, secondDef);

            _system.TryActivate("ultimate");
            Assert.IsFalse(_system.TryActivate("dodge"));

            Assert.AreSame(first, _system.ActiveAbility);

            Object.DestroyImmediate(firstDef);
            Object.DestroyImmediate(secondDef);
        }

        #endregion

        #region Cooldown

        [Test]
        public void Cooldown_DecreasesOverTime()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);
            _system.TryActivate("attack");
            _system.Deactivate("attack");

            Assert.AreEqual(1f, _system.GetCooldownRemaining("attack"), 0.01f);

            _system.Tick(0.5f);
            Assert.AreEqual(0.5f, _system.GetCooldownRemaining("attack"), 0.01f);
        }

        [Test]
        public void Cooldown_AllowsReactivation_WhenExpired()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);
            _system.TryActivate("attack");
            _system.Deactivate("attack");

            // Tick past cooldown
            _system.Tick(1.1f);

            Assert.IsTrue(_system.TryActivate("attack"));
            Assert.AreEqual(2, ability.ActivateCallCount);
        }

        [Test]
        public void Cooldown_FiresEvent_WhenComplete()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);
            _system.TryActivate("attack");
            _system.Deactivate("attack");

            IAbility eventAbility = null;
            _system.OnAbilityCooldownComplete += a => eventAbility = a;

            _system.Tick(1.1f);

            Assert.AreSame(ability, eventAbility);
        }

        [Test]
        public void NoCooldown_AbilityReactivatesImmediately()
        {
            var def = CreateDef(cooldown: 0f);
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, def);

            _system.TryActivate("attack");
            _system.Deactivate("attack");

            Assert.IsTrue(_system.TryActivate("attack"));

            Object.DestroyImmediate(def);
        }

        #endregion

        #region Deactivation

        [Test]
        public void Deactivate_StartsCooldown()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);
            _system.TryActivate("attack");

            _system.Deactivate("attack");

            Assert.AreEqual(1f, _system.GetCooldownRemaining("attack"), 0.01f);
            Assert.IsFalse(_system.HasActiveAbility);
        }

        [Test]
        public void Deactivate_FiresEvent()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);
            _system.TryActivate("attack");

            IAbility eventAbility = null;
            _system.OnAbilityDeactivated += a => eventAbility = a;

            _system.Deactivate("attack");

            Assert.AreSame(ability, eventAbility);
        }

        #endregion

        #region Cancel

        [Test]
        public void Cancel_FiresCancelEvent_NotDeactivateEvent()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);
            _system.TryActivate("attack");

            IAbility cancelledAbility = null;
            IAbility deactivatedAbility = null;
            _system.OnAbilityCancelled += a => cancelledAbility = a;
            _system.OnAbilityDeactivated += a => deactivatedAbility = a;

            _system.Cancel("attack");

            Assert.AreSame(ability, cancelledAbility);
            Assert.IsNull(deactivatedAbility); // Cancelled, not deactivated
        }

        [Test]
        public void Cancel_StartsCooldown()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);
            _system.TryActivate("attack");

            _system.Cancel("attack");

            Assert.AreEqual(1f, _system.GetCooldownRemaining("attack"), 0.01f);
        }

        #endregion

        #region Tick

        [Test]
        public void Tick_CallsAbilityTick_WhenActive()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);
            _system.TryActivate("attack");

            _system.Tick(0.016f);

            Assert.AreEqual(1, ability.TickCallCount);
            Assert.AreEqual(0.016f, ability.LastTickDeltaTime, 0.001f);
        }

        [Test]
        public void Tick_DoesNotCallTick_WhenInactive()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);

            _system.Tick(0.016f);

            Assert.AreEqual(0, ability.TickCallCount);
        }

        #endregion

        #region Events

        [Test]
        public void ActivateEvent_FiredOnActivation()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);

            IAbility eventAbility = null;
            _system.OnAbilityActivated += a => eventAbility = a;

            _system.TryActivate("attack");

            Assert.AreSame(ability, eventAbility);
        }

        #endregion

        #region Queries

        [Test]
        public void GetAbility_ReturnsAbility_WhenRegistered()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);

            Assert.AreSame(ability, _system.GetAbility("attack"));
        }

        [Test]
        public void GetAbility_ReturnsNull_WhenNotRegistered()
        {
            Assert.IsNull(_system.GetAbility("nonexistent"));
        }

        [Test]
        public void GetCooldownRemaining_ReturnsZero_WhenReady()
        {
            var ability = new MockAbility { Id = "attack" };
            _system.RegisterAbility(ability, _defaultDef);

            Assert.AreEqual(0f, _system.GetCooldownRemaining("attack"));
        }

        [Test]
        public void ActiveAbility_IsNull_WhenNoneActive()
        {
            Assert.IsNull(_system.ActiveAbility);
            Assert.IsFalse(_system.HasActiveAbility);
        }

        #endregion
    }
}
