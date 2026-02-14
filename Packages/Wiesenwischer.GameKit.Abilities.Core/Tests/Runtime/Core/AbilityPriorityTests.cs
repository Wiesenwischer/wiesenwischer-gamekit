using NUnit.Framework;

namespace Wiesenwischer.GameKit.Abilities.Core.Tests
{
    [TestFixture]
    public class AbilityPriorityTests
    {
        [Test]
        public void Constants_HaveCorrectOrder()
        {
            Assert.Less(AbilityPriority.Interaction, AbilityPriority.Utility);
            Assert.Less(AbilityPriority.Utility, AbilityPriority.Attack);
            Assert.Less(AbilityPriority.Attack, AbilityPriority.Dodge);
            Assert.Less(AbilityPriority.Dodge, AbilityPriority.Ultimate);
            Assert.Less(AbilityPriority.Ultimate, AbilityPriority.ForcedStatus);
        }

        [Test]
        public void ForcedStatus_IsHighestPriority()
        {
            Assert.AreEqual(100, AbilityPriority.ForcedStatus);
            Assert.Greater(AbilityPriority.ForcedStatus, AbilityPriority.Ultimate);
        }

        [Test]
        public void Interaction_IsLowestPriority()
        {
            Assert.AreEqual(1, AbilityPriority.Interaction);
        }
    }
}
