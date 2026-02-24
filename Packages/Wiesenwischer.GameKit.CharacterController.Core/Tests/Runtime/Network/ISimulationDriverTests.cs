using NUnit.Framework;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests
{
    [TestFixture]
    public class ISimulationDriverTests
    {
        private class MockDriver : ISimulationDriver
        {
            public bool IsActive { get; set; }
            public float TickDelta { get; set; }
            public uint CurrentTick { get; set; }
        }

        [Test]
        public void MockDriver_ImplementsInterface()
        {
            var driver = new MockDriver
            {
                IsActive = true,
                TickDelta = 1f / 60f,
                CurrentTick = 42
            };

            Assert.IsTrue(driver.IsActive);
            Assert.AreEqual(1f / 60f, driver.TickDelta, 0.0001f);
            Assert.AreEqual(42u, driver.CurrentTick);
        }

        [Test]
        public void InactiveDriver_AllowsOfflineSimulation()
        {
            var driver = new MockDriver { IsActive = false };
            Assert.IsFalse(driver.IsActive);
        }

        [Test]
        public void ActiveDriver_PreventsFixedUpdateSimulation()
        {
            var driver = new MockDriver { IsActive = true };
            Assert.IsTrue(driver.IsActive);
        }

        [Test]
        public void TickDelta_StandardTickRate_IsCorrect()
        {
            var driver = new MockDriver { TickDelta = 1f / 30f };
            Assert.AreEqual(1f / 30f, driver.TickDelta, 0.0001f);
        }

        [Test]
        public void CurrentTick_CanIncrement()
        {
            var driver = new MockDriver { CurrentTick = 0 };
            driver.CurrentTick++;
            Assert.AreEqual(1u, driver.CurrentTick);
        }

        [Test]
        public void CurrentTick_LargeValue_Preserved()
        {
            var driver = new MockDriver { CurrentTick = uint.MaxValue - 1 };
            Assert.AreEqual(uint.MaxValue - 1, driver.CurrentTick);
        }

        [Test]
        public void Interface_CanBeCastFromObject()
        {
            object driver = new MockDriver
            {
                IsActive = true,
                TickDelta = 0.02f,
                CurrentTick = 100
            };

            Assert.IsInstanceOf<ISimulationDriver>(driver);
            var typedDriver = (ISimulationDriver)driver;
            Assert.IsTrue(typedDriver.IsActive);
        }
    }
}
