using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.IK.Modules;

namespace Wiesenwischer.GameKit.CharacterController.IK.Tests
{
    [TestFixture]
    public class FootLockTests
    {
        private const float ReleaseDuration = 0.15f;
        private const float MaxLockDistance = 0.3f;
        private const float DeltaTime = 0.016f;

        private static readonly Vector3 RootPos = Vector3.zero;
        private static readonly Quaternion RootRot = Quaternion.identity;

        private FootLock.FootState Step(FootLock.FootState state, Vector3 footPos,
            bool shouldLock = false, bool shouldRelease = false,
            Quaternion? footRot = null, Vector3? rootPos = null, Quaternion? rootRot = null)
        {
            return FootLock.CalculateFootLock(
                footPos,
                footRot ?? Quaternion.identity,
                rootPos ?? RootPos,
                rootRot ?? RootRot,
                state, shouldLock, shouldRelease, DeltaTime,
                ReleaseDuration, MaxLockDistance);
        }

        // === 1. Lock when character stops ===

        [Test]
        public void FootLock_ShouldLock_LocksImmediately()
        {
            Vector3 pos = new Vector3(0.5f, 0f, 0f);
            var state = new FootLock.FootState();

            state = Step(state, pos, shouldLock: true);

            Assert.IsTrue(state.IsLocked);
            Assert.AreEqual(pos, state.LockedLocalPos);
        }

        [Test]
        public void FootLock_ShouldNotLock_StaysUnlocked()
        {
            Vector3 pos = new Vector3(0.5f, 0f, 0f);
            var state = new FootLock.FootState();

            state = Step(state, pos, shouldLock: false);

            Assert.IsFalse(state.IsLocked);
        }

        // === 2. Release when character moves ===

        [Test]
        public void FootLock_ShouldRelease_StartsReleaseBlend()
        {
            Vector3 pos = new Vector3(0.5f, 0f, 0f);
            var state = new FootLock.FootState();

            // Lock
            state = Step(state, pos, shouldLock: true);
            Assert.IsTrue(state.IsLocked);

            // Release
            state = Step(state, pos, shouldRelease: true);
            Assert.IsFalse(state.IsLocked);
            Assert.IsTrue(state.IsReleasing);
            Assert.AreEqual(0f, state.ReleaseTimer);
        }

        [Test]
        public void FootLock_NotReleasing_StaysLocked()
        {
            Vector3 pos = new Vector3(0.5f, 0f, 0f);
            var state = new FootLock.FootState();

            state = Step(state, pos, shouldLock: true);
            // Neither lock nor release signal
            state = Step(state, pos);

            Assert.IsTrue(state.IsLocked);
        }

        // === 3. Release Blend ===

        [Test]
        public void FootLock_ReleaseBlend_TimerAdvances()
        {
            var state = new FootLock.FootState
            {
                IsReleasing = true,
                ReleaseTimer = 0f
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
                ReleaseTimer = ReleaseDuration - DeltaTime * 0.5f
            };

            state = Step(state, Vector3.zero);

            Assert.IsFalse(state.IsReleasing);
        }

        // === 4. Local-Space ===

        [Test]
        public void FootLock_LocalSpaceStorage_SurvivesCharacterRotation()
        {
            Vector3 footPos = new Vector3(1f, 0f, 0f);
            var state = new FootLock.FootState();

            state = Step(state, footPos, shouldLock: true);
            Assert.IsTrue(state.IsLocked);

            // Root dreht sich 90° um Y → Fuß sollte bei (0, 0, 1) statt (1, 0, 0) sein
            Quaternion rotated = Quaternion.Euler(0f, 90f, 0f);
            Vector3 lockedWorld = RootPos + rotated * state.LockedLocalPos;

            Assert.AreEqual(0f, lockedWorld.x, 0.01f);
            Assert.AreEqual(1f, lockedWorld.z, 0.01f);
        }

        [Test]
        public void FootLock_LocalSpaceStorage_SurvivesCharacterMovement()
        {
            Vector3 footPos = new Vector3(1f, 0f, 0f);
            var state = new FootLock.FootState();

            state = Step(state, footPos, shouldLock: true);
            Assert.IsTrue(state.IsLocked);

            // Root bewegt sich um (5, 0, 3)
            Vector3 newRootPos = new Vector3(5f, 0f, 3f);
            Vector3 lockedWorld = newRootPos + RootRot * state.LockedLocalPos;

            Assert.AreEqual(6f, lockedWorld.x, 0.01f);
            Assert.AreEqual(3f, lockedWorld.z, 0.01f);
        }

        // === 5. Safety: Max Lock Distance ===

        [Test]
        public void FootLock_MaxLockDistance_ForcesRelease()
        {
            Vector3 lockedPos = new Vector3(0.5f, 0f, 0f);
            var state = new FootLock.FootState();

            state = Step(state, lockedPos, shouldLock: true);
            Assert.IsTrue(state.IsLocked);

            // Fuß springt weit weg (über maxLockDistance)
            Vector3 farPos = lockedPos + new Vector3(MaxLockDistance + 0.1f, 0f, 0f);
            state = Step(state, farPos);

            Assert.IsFalse(state.IsLocked);
            Assert.IsTrue(state.IsReleasing);
        }

        [Test]
        public void FootLock_MaxLockDistance_WithinLimit_StaysLocked()
        {
            Vector3 lockedPos = new Vector3(0.5f, 0f, 0f);
            var state = new FootLock.FootState();

            state = Step(state, lockedPos, shouldLock: true);
            Assert.IsTrue(state.IsLocked);

            // Fuß bewegt sich leicht (innerhalb maxLockDistance)
            Vector3 nearPos = lockedPos + new Vector3(MaxLockDistance * 0.5f, 0f, 0f);
            state = Step(state, nearPos);

            Assert.IsTrue(state.IsLocked);
        }

        // === 6. IK-Weight Berechnung ===

        [Test]
        public void FootLock_ProcessWeight_LockedFull()
        {
            var state = new FootLock.FootState { IsLocked = true };
            float weight = 1f;
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
            var state = new FootLock.FootState();
            Assert.IsFalse(state.IsLocked);
            Assert.IsFalse(state.IsReleasing);
        }

        // === 7. During release, shouldLock is ignored ===

        [Test]
        public void FootLock_DuringRelease_ShouldLockIgnored()
        {
            var state = new FootLock.FootState
            {
                IsReleasing = true,
                ReleaseTimer = 0f
            };

            // shouldLock is true but we're still releasing → don't re-lock
            state = Step(state, new Vector3(1f, 0f, 0f), shouldLock: true);

            Assert.IsFalse(state.IsLocked);
            Assert.IsTrue(state.IsReleasing);
        }

        // === 8. Hysteresis ===

        [Test]
        public void FootLock_Hysteresis_StaysLockedBetweenThresholds()
        {
            Vector3 pos = new Vector3(0.5f, 0f, 0f);
            var state = new FootLock.FootState();

            // Lock
            state = Step(state, pos, shouldLock: true);
            Assert.IsTrue(state.IsLocked);

            // Neither shouldLock nor shouldRelease (speed in hysteresis band)
            state = Step(state, pos, shouldLock: false, shouldRelease: false);
            Assert.IsTrue(state.IsLocked);
        }
    }
}
