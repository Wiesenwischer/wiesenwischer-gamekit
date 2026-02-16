using NUnit.Framework;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests
{
    [TestFixture]
    public class TickSystemNetworkTests
    {
        [Test]
        public void SetTick_AllowsRollback()
        {
            var tickSystem = new TickSystem(60f);

            for (int i = 0; i < 10; i++)
                tickSystem.Update(tickSystem.TickDelta);

            Assert.AreEqual(10, tickSystem.CurrentTick);

            tickSystem.SetTick(5);
            Assert.AreEqual(5, tickSystem.CurrentTick);
        }

        [Test]
        public void TickDelta_MatchesTick60Hz()
        {
            var tickSystem = new TickSystem(60f);
            Assert.AreEqual(1f / 60f, tickSystem.TickDelta, 0.0001f);
        }

        [Test]
        public void TickToTime_ConvertsCorrectly()
        {
            var tickSystem = new TickSystem(60f);
            float time = tickSystem.TickToTime(60);
            Assert.AreEqual(1f, time, 0.001f);
        }
    }
}
