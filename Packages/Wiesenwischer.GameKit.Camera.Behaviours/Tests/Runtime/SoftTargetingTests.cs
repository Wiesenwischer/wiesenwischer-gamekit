using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours.Tests
{
    [TestFixture]
    public class SoftTargetingTests
    {
        private SoftTargetingBehaviour _behaviour;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("SoftTargetingTest");
            _behaviour = _go.AddComponent<SoftTargetingBehaviour>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        private CameraContext CreateContext(
            Vector3? velocity = null,
            Transform lookTarget = null)
        {
            return new CameraContext
            {
                AnchorPosition = Vector3.zero,
                CharacterVelocity = velocity ?? Vector3.zero,
                CharacterForward = Vector3.forward,
                LookTarget = lookTarget,
                DeltaTime = 0.016f,
                Input = default
            };
        }

        private void SimulateFrames(ref CameraState state, CameraContext ctx, int frames = 100)
        {
            for (int i = 0; i < frames; i++)
                _behaviour.UpdateState(ref state, ctx);
        }

        [Test]
        public void NoTargetNoMovement_NoBias()
        {
            var state = CameraState.Default;
            float initialYaw = state.Yaw;
            var ctx = CreateContext();

            _behaviour.UpdateState(ref state, ctx);

            Assert.AreEqual(initialYaw, state.Yaw, 0.001f);
        }

        [Test]
        public void Movement_AddsYawBias()
        {
            var state = CameraState.Default;
            state.Yaw = 0f; // Facing forward (Z+)
            var ctx = CreateContext(velocity: new Vector3(5f, 0f, 0f)); // Moving right

            SimulateFrames(ref state, ctx, 200);

            // Yaw should be biased toward movement direction (right = positive yaw)
            Assert.Greater(state.Yaw, 0.5f);
        }

        [Test]
        public void LookTarget_AddsTargetBias()
        {
            var targetGo = new GameObject("Target");
            targetGo.transform.position = new Vector3(-10f, 0f, 10f); // Left-forward

            var state = CameraState.Default;
            state.Yaw = 0f;
            var ctx = CreateContext(lookTarget: targetGo.transform);

            SimulateFrames(ref state, ctx, 200);

            // Yaw should be biased toward target (left = negative yaw)
            Assert.Less(state.Yaw, -0.5f);

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void LookTarget_OutOfRange_NoBias()
        {
            var targetGo = new GameObject("Target");
            targetGo.transform.position = new Vector3(100f, 0f, 0f); // Very far

            var state = CameraState.Default;
            float initialYaw = state.Yaw;
            var ctx = CreateContext(lookTarget: targetGo.transform);

            SimulateFrames(ref state, ctx, 100);

            // Target out of range → no bias
            Assert.AreEqual(initialYaw, state.Yaw, 0.5f);

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void LookTarget_HasPriorityOverMovement()
        {
            var targetGo = new GameObject("Target");
            targetGo.transform.position = new Vector3(-10f, 0f, 10f); // Left

            var state = CameraState.Default;
            state.Yaw = 0f;
            // Movement to the right, target to the left
            var ctx = CreateContext(
                velocity: new Vector3(5f, 0f, 0f),
                lookTarget: targetGo.transform);

            SimulateFrames(ref state, ctx, 200);

            // Target bias wins → yaw goes left (negative)
            Assert.Less(state.Yaw, 0f);

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void BiasIsSmoothed()
        {
            var targetGo = new GameObject("Target");
            targetGo.transform.position = new Vector3(10f, 0f, 10f);

            var state = CameraState.Default;
            state.Yaw = 0f;
            var ctx = CreateContext(lookTarget: targetGo.transform);

            // First frame: bias should not be at full strength yet
            _behaviour.UpdateState(ref state, ctx);
            float firstFrameYaw = state.Yaw;

            // More frames: bias should increase
            SimulateFrames(ref state, ctx, 50);
            float laterYaw = state.Yaw;

            Assert.Greater(Mathf.Abs(laterYaw), Mathf.Abs(firstFrameYaw));

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void Disabled_NoBias()
        {
            _behaviour.enabled = false;
            Assert.IsFalse(_behaviour.IsActive);
        }

        [Test]
        public void ApplyPreset_SetsValues()
        {
            var preset = ScriptableObject.CreateInstance<CameraPreset>();
            preset.SoftTargetingEnabled = false;
            preset.MovementBiasStrength = 10f;
            preset.TargetBiasStrength = 25f;
            preset.SoftTargetRange = 30f;
            preset.SoftTargetDamping = 0.2f;

            _behaviour.ApplyPreset(preset);

            Assert.IsFalse(_behaviour.enabled);

            Object.DestroyImmediate(preset);
        }
    }
}
