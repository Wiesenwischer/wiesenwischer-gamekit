using NUnit.Framework;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests
{
    [TestFixture]
    public class NetworkRoleTests
    {
        [Test]
        public void OfflineRole_IsOwner_ReturnsTrue()
        {
            Assert.IsTrue(OfflineNetworkRole.Instance.IsOwner);
        }

        [Test]
        public void OfflineRole_IsNetworkActive_ReturnsFalse()
        {
            Assert.IsFalse(OfflineNetworkRole.Instance.IsNetworkActive);
        }

        [Test]
        public void OfflineRole_IsClient_ReturnsTrue()
        {
            Assert.IsTrue(OfflineNetworkRole.Instance.IsClient);
        }

        [Test]
        public void OfflineRole_IsServer_ReturnsFalse()
        {
            Assert.IsFalse(OfflineNetworkRole.Instance.IsServer);
        }

        [Test]
        public void OfflineRole_IsSingleton()
        {
            Assert.AreSame(
                OfflineNetworkRole.Instance,
                OfflineNetworkRole.Instance);
        }
    }
}
