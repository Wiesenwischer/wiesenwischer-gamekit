using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Network.Tests
{
    [TestFixture]
    public class CharacterReconcileDataTests
    {
        [Test]
        public void GetTick_SetTick_RoundTrip()
        {
            var data = new CharacterReconcileData();
            data.SetTick(100);
            Assert.AreEqual(100u, data.GetTick());
        }

        [Test]
        public void SetTick_LargeValue_Preserved()
        {
            var data = new CharacterReconcileData();
            data.SetTick(uint.MaxValue);
            Assert.AreEqual(uint.MaxValue, data.GetTick());
        }

        [Test]
        public void Position_IsPreserved()
        {
            var pos = new Vector3(1.5f, 2.0f, 3.5f);
            var data = new CharacterReconcileData { Position = pos };
            Assert.AreEqual(pos, data.Position);
        }

        [Test]
        public void Position_NegativeValues_IsPreserved()
        {
            var pos = new Vector3(-100f, -50f, -200f);
            var data = new CharacterReconcileData { Position = pos };
            Assert.AreEqual(pos, data.Position);
        }

        [Test]
        public void Rotation_IsPreserved()
        {
            var data = new CharacterReconcileData { Rotation = 270.5f };
            Assert.AreEqual(270.5f, data.Rotation, 0.001f);
        }

        [Test]
        public void Velocity_IsPreserved()
        {
            var vel = new Vector3(5f, 0f, -3f);
            var data = new CharacterReconcileData { Velocity = vel };
            Assert.AreEqual(vel, data.Velocity);
        }

        [Test]
        public void VerticalVelocity_IsPreserved()
        {
            var data = new CharacterReconcileData { VerticalVelocity = -9.81f };
            Assert.AreEqual(-9.81f, data.VerticalVelocity, 0.001f);
        }

        [Test]
        public void VerticalVelocity_PositiveJump_IsPreserved()
        {
            var data = new CharacterReconcileData { VerticalVelocity = 12.5f };
            Assert.AreEqual(12.5f, data.VerticalVelocity, 0.001f);
        }

        [Test]
        public void BooleanFlags_DefaultFalse()
        {
            var data = new CharacterReconcileData();
            Assert.IsFalse(data.IsGrounded);
            Assert.IsFalse(data.IsCrouching);
            Assert.IsFalse(data.ShouldWalk);
        }

        [Test]
        public void BooleanFlags_CanBeSet()
        {
            var data = new CharacterReconcileData
            {
                IsGrounded = true,
                IsCrouching = true,
                ShouldWalk = true
            };

            Assert.IsTrue(data.IsGrounded);
            Assert.IsTrue(data.IsCrouching);
            Assert.IsTrue(data.ShouldWalk);
        }

        [Test]
        public void MovementStateIndex_ByteRange()
        {
            var data = new CharacterReconcileData { MovementStateIndex = 255 };
            Assert.AreEqual(255, data.MovementStateIndex);
        }

        [Test]
        public void MovementStateIndex_Zero()
        {
            var data = new CharacterReconcileData { MovementStateIndex = 0 };
            Assert.AreEqual(0, data.MovementStateIndex);
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var data = new CharacterReconcileData
            {
                Position = Vector3.one,
                Rotation = 90f,
                IsGrounded = true
            };
            data.SetTick(50);

            Assert.DoesNotThrow(() => data.Dispose());
        }

        [Test]
        public void AllFields_CombinedRoundTrip()
        {
            var data = new CharacterReconcileData
            {
                Position = new Vector3(10f, 0.5f, -20f),
                Rotation = 180f,
                Velocity = new Vector3(3f, 0f, -1f),
                VerticalVelocity = -5f,
                IsGrounded = true,
                IsCrouching = false,
                ShouldWalk = true,
                MovementStateIndex = 42
            };
            data.SetTick(12345);

            Assert.AreEqual(12345u, data.GetTick());
            Assert.AreEqual(10f, data.Position.x, 0.001f);
            Assert.AreEqual(0.5f, data.Position.y, 0.001f);
            Assert.AreEqual(-20f, data.Position.z, 0.001f);
            Assert.AreEqual(180f, data.Rotation, 0.001f);
            Assert.AreEqual(3f, data.Velocity.x, 0.001f);
            Assert.AreEqual(-5f, data.VerticalVelocity, 0.001f);
            Assert.IsTrue(data.IsGrounded);
            Assert.IsFalse(data.IsCrouching);
            Assert.IsTrue(data.ShouldWalk);
            Assert.AreEqual(42, data.MovementStateIndex);
        }
    }
}
