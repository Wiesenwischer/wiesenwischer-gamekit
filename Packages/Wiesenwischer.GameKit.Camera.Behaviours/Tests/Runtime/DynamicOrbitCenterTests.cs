using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours.Tests
{
    [TestFixture]
    public class DynamicOrbitCenterTests
    {
        private DynamicOrbitCenterBehaviour _behaviour;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("DynamicOrbitTest");
            _behaviour = _go.AddComponent<DynamicOrbitCenterBehaviour>();
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
            {
                ctx.AnchorPosition = Vector3.zero; // Reset each frame
                _behaviour.UpdateState(ref state, ctx);
            }
        }

        [Test]
        public void Idle_NoOffsetApplied()
        {
            var state = CameraState.Default;
            var ctx = CreateContext();

            _behaviour.UpdateState(ref state, ctx);

            // After one frame with no velocity, anchor should be near zero
            Assert.AreEqual(0f, ctx.AnchorPosition.magnitude, 0.01f);
        }

        [Test]
        public void Movement_ShiftsAnchorForward()
        {
            var state = CameraState.Default;
            var ctx = CreateContext(velocity: new Vector3(0f, 0f, 5f));

            SimulateFrames(ref state, ctx, 200);

            // After convergence, anchor should be shifted in Z direction
            Assert.Greater(ctx.AnchorPosition.z, 0.1f);
        }

        [Test]
        public void LookTarget_ShiftsTowardsMidpoint()
        {
            var targetGo = new GameObject("Target");
            targetGo.transform.position = new Vector3(0f, 0f, 10f);

            var state = CameraState.Default;
            var ctx = CreateContext(lookTarget: targetGo.transform);

            SimulateFrames(ref state, ctx, 200);

            // Anchor should be shifted toward target
            Assert.Greater(ctx.AnchorPosition.z, 0.1f);

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void LookTarget_HasPriorityOverMovement()
        {
            var targetGo = new GameObject("Target");
            targetGo.transform.position = new Vector3(10f, 0f, 0f);

            var state = CameraState.Default;
            var ctx = CreateContext(
                velocity: new Vector3(0f, 0f, 5f),
                lookTarget: targetGo.transform);

            SimulateFrames(ref state, ctx, 200);

            // Should shift toward target (X), not forward movement (Z)
            Assert.Greater(ctx.AnchorPosition.x, 0.1f);

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void BelowMinSpeed_NoForwardBias()
        {
            var state = CameraState.Default;
            var ctx = CreateContext(velocity: new Vector3(0f, 0f, 0.1f));

            SimulateFrames(ref state, ctx, 100);

            // Very slow movement → below minSpeed → no bias
            Assert.AreEqual(0f, ctx.AnchorPosition.magnitude, 0.05f);
        }

        [Test]
        public void Snap_ResetsOffset()
        {
            var state = CameraState.Default;
            var ctx = CreateContext(velocity: new Vector3(0f, 0f, 5f));

            // Build up offset
            SimulateFrames(ref state, ctx, 100);

            _behaviour.Snap();

            // After snap, next frame with idle should start from zero
            ctx = CreateContext();
            _behaviour.UpdateState(ref state, ctx);

            Assert.AreEqual(0f, ctx.AnchorPosition.magnitude, 0.01f);
        }

        [Test]
        public void Disabled_NoUpdate()
        {
            _behaviour.enabled = false;

            var state = CameraState.Default;
            var ctx = CreateContext(velocity: new Vector3(0f, 0f, 5f));

            _behaviour.UpdateState(ref state, ctx);

            // IsActive returns false when disabled, but UpdateState is direct call
            // The CameraBrain checks IsActive before calling, so test IsActive
            Assert.IsFalse(_behaviour.IsActive);
        }

        [Test]
        public void ApplyPreset_SetsValues()
        {
            var preset = ScriptableObject.CreateInstance<CameraPreset>();
            preset.DynamicOrbitEnabled = false;
            preset.ForwardBias = 1.5f;
            preset.OrbitCenterDamping = 0.2f;

            _behaviour.ApplyPreset(preset);

            Assert.IsFalse(_behaviour.enabled);

            Object.DestroyImmediate(preset);
        }
    }
}
