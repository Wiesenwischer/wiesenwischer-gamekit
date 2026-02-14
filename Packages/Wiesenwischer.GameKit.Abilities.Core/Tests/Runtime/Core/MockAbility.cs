namespace Wiesenwischer.GameKit.Abilities.Core.Tests
{
    /// <summary>
    /// Configurable mock ability for unit testing the AbilitySystem.
    /// </summary>
    public class MockAbility : IAbility
    {
        public string Id { get; set; } = "mock_ability";
        public AbilityState State { get; set; } = AbilityState.Ready;
        public int Priority { get; set; } = AbilityPriority.Attack;

        // Configurable behavior
        public bool CanActivateResult { get; set; } = true;

        // Call tracking
        public int ActivateCallCount { get; private set; }
        public int DeactivateCallCount { get; private set; }
        public int TickCallCount { get; private set; }
        public float LastTickDeltaTime { get; private set; }

        public bool CanActivate(AbilityContext context) => CanActivateResult;

        public void Activate(AbilityContext context)
        {
            State = AbilityState.Active;
            ActivateCallCount++;
        }

        public void Tick(AbilityContext context, float deltaTime)
        {
            TickCallCount++;
            LastTickDeltaTime = deltaTime;
        }

        public void Deactivate(AbilityContext context)
        {
            State = AbilityState.Ready;
            DeactivateCallCount++;
        }

        public void Reset()
        {
            State = AbilityState.Ready;
            ActivateCallCount = 0;
            DeactivateCallCount = 0;
            TickCallCount = 0;
            LastTickDeltaTime = 0f;
            CanActivateResult = true;
        }
    }
}
