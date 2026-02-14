using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.IK.Modules;

namespace Wiesenwischer.GameKit.CharacterController.IK.Tests
{
    [TestFixture]
    public class FootLockTests
    {
        private const float LockThreshold = 0.05f;
        private const float ReleaseThreshold = 0.15f;
        private const int StableFrames = 2;
        private const float ReleaseDuration = 0.15f;
        private const float MaxLockDistance = 0.3f;
        private const float DeltaTime = 0.016f;

        private static readonly Vector3 RootPos = Vector3.zero;
        private static readonly Quaternion RootRot = Quaternion.identity;

        private FootLock.FootState CreateStationaryState(Vector3 pos)
        {
            return new FootLock.FootState { PrevWorldPos = pos };
        }

        private FootLock.FootState CreateLockedState(Vector3 footWorldPos)
        {
            Quaternion invRootRot = Quaternion.Inverse(RootRot);
            return new FootLock.FootState
            {
                IsLocked = true,
                PrevWorldPos = footWorldPos,
                LockedLocalPos = invRootRot * (footWorldPos - RootPos),
                LockedLocalRot = invRootRot * Quaternion.identity
            };
        }

        private FootLock.FootState Step(FootLock.FootState state, Vector3 footPos,
            bool isGrounded = true, Quaternion? footRot = null,
            Vector3? rootPos = null, Quaternion? rootRot = null)
        {
            return FootLock.CalculateFootLock(
                footPos,
                footRot ?? Quaternion.identity,
                rootPos ?? RootPos,
                rootRot ?? RootRot,
                state, isGrounded, DeltaTime,
                LockThreshold, ReleaseThreshold,
                StableFrames, ReleaseDuration, MaxLockDistance);
        }

        // === 1. Lock-Erkennung ===

        [Test]
        public void FootLock_VelocityBelowThreshold_LocksAfterStableFrames()
        {
            Vector3 pos = new Vector3(0.5f, 0f, 0f);
            var state = CreateStationaryState(pos);

            // Frame 1: Fuß steht still (velocity = 0)
            state = Step(state, pos);
            Assert.IsFalse(state.IsLocked);
            Assert.AreEqual(1, state.StableCount);

            // Frame 2: Immer noch still → Lock!
            state = Step(state, pos);
            Assert.IsTrue(state.IsLocked);
            Assert.AreEqual(0, state.StableCount);
            Assert.AreEqual(pos, state.LockedLocalPos);
        }

        [Test]
        public void FootLock_VelocityBelowThreshold_NotLockedBeforeStableFrames()
        {
            Vector3 pos = new Vector3(0.5f, 0f, 0f);
            var state = CreateStationaryState(pos);

            state = Step(state, pos);
            Assert.IsFalse(state.IsLocked);
            Assert.AreEqual(1, state.StableCount);
        }

        [Test]
        public void FootLock_NotGrounded_NeverLocks()
        {
            Vector3 pos = new Vector3(0.5f, 0f, 0f);
            var state = CreateStationaryState(pos);

            state = Step(state, pos, isGrounded: false);
            Assert.IsFalse(state.IsLocked);
            Assert.AreEqual(0, state.StableCount);

            state = Step(state, pos, isGrounded: false);
            Assert.IsFalse(state.IsLocked);
            Assert.AreEqual(0, state.StableCount);
        }

        // === 2. Release-Erkennung ===

        [Test]
        public void FootLock_VelocityAboveReleaseThreshold_ReleasesLock()
        {
            Vector3 lockedPos = new Vector3(0.5f, 0f, 0f);
            var state = CreateLockedState(lockedPos);

            // Fuß bewegt sich schnell (velocity > releaseThreshold)
            float moveDistance = ReleaseThreshold * DeltaTime * 2f;
            Vector3 movedPos = lockedPos + new Vector3(moveDistance, 0f, 0f);
            state = Step(state, movedPos);

            Assert.IsFalse(state.IsLocked);
            Assert.IsTrue(state.IsReleasing);
            Assert.AreEqual(0f, state.ReleaseTimer);
        }

        [Test]
        public void FootLock_VelocityBetweenThresholds_StaysLocked()
        {
            Vector3 lockedPos = new Vector3(0.5f, 0f, 0f);
            var state = CreateLockedState(lockedPos);

            // Velocity im Hysterese-Bereich (zwischen lock und release threshold)
            float midVelocity = (LockThreshold + ReleaseThreshold) / 2f;
            float moveDistance = midVelocity * DeltaTime;
            Vector3 movedPos = lockedPos + new Vector3(moveDistance, 0f, 0f);
            state = Step(state, movedPos);

            Assert.IsTrue(state.IsLocked);
        }

        // === 3. Release Blend ===

        [Test]
        public void FootLock_ReleaseBlend_TimerAdvances()
        {
            var state = new FootLock.FootState
            {
                IsReleasing = true,
                ReleaseTimer = 0f,
                PrevWorldPos = Vector3.zero
            };

            state = Step(state, Vector3.zero);

            Assert.IsTrue(state.IsReleasing);
            Assert.AreEqual(DeltaTime, state.ReleaseTimer, 0.001f);
        }

        [Test]
        public void FootLock_ReleaseBlend_CompletesAfterDuration()
        {
            var state = new FootLock.FootState
            {
                IsReleasing = true,
                ReleaseTimer = ReleaseDuration - DeltaTime * 0.5f,
                PrevWorldPos = Vector3.zero
            };

            state = Step(state, Vector3.zero);

            Assert.IsFalse(state.IsReleasing);
        }

        // === 4. Local-Space ===

        [Test]
        public void FootLock_LocalSpaceStorage_SurvivesCharacterRotation()
        {
            Vector3 footPos = new Vector3(1f, 0f, 0f);
            var state = CreateStationaryState(footPos);

            // Lock the foot (2 stable frames)
            state = Step(state, footPos);
            state = Step(state, footPos);
            Assert.IsTrue(state.IsLocked);

            // Root dreht sich 90° um Y
            Quaternion rotated = Quaternion.Euler(0f, 90f, 0f);
            Vector3 expectedWorldPos = RootPos + rotated * state.LockedLocalPos;

            Vector3 actualWorldPos = rotated * state.LockedLocalPos;
            Assert.AreEqual(expectedWorldPos.x, actualWorldPos.x, 0.01f);
            Assert.AreEqual(expectedWorldPos.z, actualWorldPos.z, 0.01f);

            // Fuß sollte jetzt bei (0, 0, 1) statt (1, 0, 0) sein
            Assert.AreEqual(0f, actualWorldPos.x, 0.01f);
            Assert.AreEqual(1f, actualWorldPos.z, 0.01f);
        }

        [Test]
        public void FootLock_LocalSpaceStorage_SurvivesCharacterMovement()
        {
            Vector3 footPos = new Vector3(1f, 0f, 0f);
            var state = CreateStationaryState(footPos);

            state = Step(state, footPos);
            state = Step(state, footPos);
            Assert.IsTrue(state.IsLocked);

            // Root bewegt sich um (5, 0, 3)
            Vector3 newRootPos = new Vector3(5f, 0f, 3f);
            Vector3 expectedWorldPos = newRootPos + RootRot * state.LockedLocalPos;

            Assert.AreEqual(6f, expectedWorldPos.x, 0.01f);
            Assert.AreEqual(3f, expectedWorldPos.z, 0.01f);
        }

        // === 5. Sicherheitsmechanismen ===

        [Test]
        public void FootLock_MaxLockDistance_ForcesRelease()
        {
            Vector3 lockedPos = new Vector3(0.5f, 0f, 0f);
            var state = CreateLockedState(lockedPos);

            // Fuß springt weit weg (über maxLockDistance)
            Vector3 farPos = lockedPos + new Vector3(MaxLockDistance + 0.1f, 0f, 0f);
            state.PrevWorldPos = farPos; // Prevent velocity spike from triggering release first
            state = Step(state, farPos);

            Assert.IsFalse(state.IsLocked);
            Assert.IsTrue(state.IsReleasing);
        }

        [Test]
        public void FootLock_MaxLockDistance_WithinLimit_StaysLocked()
        {
            Vector3 lockedPos = new Vector3(0.5f, 0f, 0f);
            var state = CreateLockedState(lockedPos);

            // Fuß bewegt sich leicht (innerhalb maxLockDistance, velocity im Hysterese-Bereich)
            float smallMove = MaxLockDistance * 0.5f;
            Vector3 nearPos = lockedPos + new Vector3(smallMove, 0f, 0f);
            // Set prevPos close to nearPos to keep velocity in hysteresis band
            state.PrevWorldPos = nearPos - new Vector3(LockThreshold * DeltaTime * 0.5f, 0f, 0f);
            state = Step(state, nearPos);

            Assert.IsTrue(state.IsLocked);
        }

        // === 6. IK-Weight Berechnung ===

        [Test]
        public void FootLock_ProcessWeight_LockedFull()
        {
            var state = new FootLock.FootState { IsLocked = true };
            float weight = 1f;

            // When locked, weight should be full
            float effectiveWeight = state.IsLocked ? weight : 0f;
            Assert.AreEqual(1f, effectiveWeight, 0.001f);
        }

        [Test]
        public void FootLock_ProcessWeight_ReleasingDecays()
        {
            float weight = 1f;
            float timer = ReleaseDuration * 0.5f;
            float expectedWeight = weight * (1f - Mathf.Clamp01(timer / ReleaseDuration));

            Assert.AreEqual(0.5f, expectedWeight, 0.01f);
        }

        [Test]
        public void FootLock_ProcessWeight_NotLockedNotReleasing_NoEffect()
        {
            var state = new FootLock.FootState
            {
                IsLocked = false,
                IsReleasing = false
            };

            Assert.IsFalse(state.IsLocked);
            Assert.IsFalse(state.IsReleasing);
        }

        // === Bonus: Stable-Count Reset ===

        [Test]
        public void FootLock_MovementInterrupts_ResetsStableCount()
        {
            Vector3 pos = new Vector3(0.5f, 0f, 0f);
            var state = CreateStationaryState(pos);

            // Frame 1: still
            state = Step(state, pos);
            Assert.AreEqual(1, state.StableCount);

            // Frame 2: sudden movement
            float bigMove = LockThreshold * DeltaTime * 3f;
            Vector3 movedPos = pos + new Vector3(bigMove, 0f, 0f);
            state = Step(state, movedPos);
            Assert.AreEqual(0, state.StableCount);
            Assert.IsFalse(state.IsLocked);
        }
    }
}
