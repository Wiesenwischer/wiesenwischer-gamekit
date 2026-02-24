using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Prediction;

namespace Wiesenwischer.GameKit.Network.Tests
{
    [TestFixture]
    public class MoveReplicateDataTests
    {
        [Test]
        public void GetTick_SetTick_RoundTrip()
        {
            var data = new MoveReplicateData();
            data.SetTick(42);
            Assert.AreEqual(42u, data.GetTick());
        }

        [Test]
        public void SetTick_LargeValue_Preserved()
        {
            var data = new MoveReplicateData();
            data.SetTick(uint.MaxValue);
            Assert.AreEqual(uint.MaxValue, data.GetTick());
        }

        [Test]
        public void AllFields_DefaultValues()
        {
            var data = new MoveReplicateData();
            Assert.AreEqual(Vector2.zero, data.MoveDirection);
            Assert.AreEqual(0f, data.CameraYaw);
            Assert.AreEqual(0f, data.CharacterRotation);
            Assert.AreEqual(0f, data.SpeedModifier);
            Assert.IsFalse(data.JumpRequested);
            Assert.IsFalse(data.JumpCutRequested);
            Assert.IsFalse(data.ResetVerticalRequested);
        }

        [Test]
        public void CameraYaw_IsPreserved()
        {
            var data = new MoveReplicateData { CameraYaw = 135.5f };
            Assert.AreEqual(135.5f, data.CameraYaw, 0.001f);
        }

        [Test]
        public void CameraYaw_NegativeValue_IsPreserved()
        {
            var data = new MoveReplicateData { CameraYaw = -90f };
            Assert.AreEqual(-90f, data.CameraYaw, 0.001f);
        }

        [Test]
        public void CameraYaw_FullRotation_IsPreserved()
        {
            var data = new MoveReplicateData { CameraYaw = 359.9f };
            Assert.AreEqual(359.9f, data.CameraYaw, 0.001f);
        }

        [Test]
        public void MoveDirection_IsPreserved()
        {
            var dir = new Vector2(0.707f, 0.707f);
            var data = new MoveReplicateData { MoveDirection = dir };
            Assert.AreEqual(dir.x, data.MoveDirection.x, 0.001f);
            Assert.AreEqual(dir.y, data.MoveDirection.y, 0.001f);
        }

        [Test]
        public void CharacterRotation_IsPreserved()
        {
            var data = new MoveReplicateData { CharacterRotation = 270f };
            Assert.AreEqual(270f, data.CharacterRotation, 0.001f);
        }

        [Test]
        public void Buttons_FlagsPreserved()
        {
            #pragma warning disable CS0612
            var buttons = ControllerButtons.Jump | ControllerButtons.Sprint;
            #pragma warning restore CS0612
            var data = new MoveReplicateData { Buttons = buttons };
            Assert.AreEqual(buttons, data.Buttons);
        }

        [Test]
        public void SpeedModifier_IsPreserved()
        {
            var data = new MoveReplicateData { SpeedModifier = 1.5f };
            Assert.AreEqual(1.5f, data.SpeedModifier, 0.001f);
        }

        [Test]
        public void OneShotEvents_CanBeSet()
        {
            var data = new MoveReplicateData
            {
                JumpRequested = true,
                JumpCutRequested = true,
                ResetVerticalRequested = true
            };

            Assert.IsTrue(data.JumpRequested);
            Assert.IsTrue(data.JumpCutRequested);
            Assert.IsTrue(data.ResetVerticalRequested);
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var data = new MoveReplicateData
            {
                MoveDirection = Vector2.one,
                CameraYaw = 90f,
                JumpRequested = true
            };
            data.SetTick(100);

            Assert.DoesNotThrow(() => data.Dispose());
        }

        [Test]
        public void AllFields_CombinedRoundTrip()
        {
            #pragma warning disable CS0612
            var data = new MoveReplicateData
            {
                MoveDirection = new Vector2(0.5f, -0.3f),
                CameraYaw = 180f,
                CharacterRotation = 45f,
                Buttons = ControllerButtons.Jump | ControllerButtons.Crouch,
                SpeedModifier = 0.5f,
                JumpRequested = true,
                JumpCutRequested = false,
                ResetVerticalRequested = true
            };
            data.SetTick(999);
            #pragma warning restore CS0612

            Assert.AreEqual(999u, data.GetTick());
            Assert.AreEqual(0.5f, data.MoveDirection.x, 0.001f);
            Assert.AreEqual(-0.3f, data.MoveDirection.y, 0.001f);
            Assert.AreEqual(180f, data.CameraYaw, 0.001f);
            Assert.AreEqual(45f, data.CharacterRotation, 0.001f);
            Assert.AreEqual(0.5f, data.SpeedModifier, 0.001f);
            Assert.IsTrue(data.JumpRequested);
            Assert.IsFalse(data.JumpCutRequested);
            Assert.IsTrue(data.ResetVerticalRequested);
        }
    }
}
