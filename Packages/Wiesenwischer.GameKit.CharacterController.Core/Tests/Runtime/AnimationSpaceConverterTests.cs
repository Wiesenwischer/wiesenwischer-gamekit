using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests
{
    [TestFixture]
    public class AnimationSpaceConverterTests
    {
        private GameObject _characterGO;
        private Transform _character;

        [SetUp]
        public void SetUp()
        {
            _characterGO = new GameObject("TestCharacter");
            _character = _characterGO.transform;
            _character.position = Vector3.zero;
            _character.rotation = Quaternion.identity;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_characterGO);
        }

        #region WorldToLocal

        [Test]
        public void WorldToLocal_ForwardMovement_ReturnsPositiveZ()
        {
            var result = AnimationSpaceConverter.WorldToLocal(Vector3.forward, _character);

            Assert.Greater(result.z, 0.9f);
            Assert.AreEqual(0f, result.x, 0.01f);
        }

        [Test]
        public void WorldToLocal_RightMovement_ReturnsPositiveX()
        {
            var result = AnimationSpaceConverter.WorldToLocal(Vector3.right, _character);

            Assert.Greater(result.x, 0.9f);
            Assert.AreEqual(0f, result.z, 0.01f);
        }

        [Test]
        public void WorldToLocal_BackwardMovement_ReturnsNegativeZ()
        {
            var result = AnimationSpaceConverter.WorldToLocal(Vector3.back, _character);

            Assert.Less(result.z, -0.9f);
        }

        [Test]
        public void WorldToLocal_LeftMovement_ReturnsNegativeX()
        {
            var result = AnimationSpaceConverter.WorldToLocal(Vector3.left, _character);

            Assert.Less(result.x, -0.9f);
        }

        [Test]
        public void WorldToLocal_ZeroInput_ReturnsZero()
        {
            var result = AnimationSpaceConverter.WorldToLocal(Vector3.zero, _character);

            Assert.AreEqual(Vector3.zero, result);
        }

        [Test]
        public void WorldToLocal_RotatedCharacter_CorrectlyTransforms()
        {
            // Character schaut nach rechts (90° Drehung)
            _character.rotation = Quaternion.Euler(0, 90, 0);

            // Welt-Vorwärts (Z+) sollte für den Character jetzt "links" sein (X-)
            var result = AnimationSpaceConverter.WorldToLocal(Vector3.forward, _character);

            Assert.Less(result.x, -0.9f);
            Assert.AreEqual(0f, result.z, 0.01f);
        }

        #endregion

        #region GetTurnAngle

        [Test]
        public void GetTurnAngle_SameDirection_ReturnsZero()
        {
            float angle = AnimationSpaceConverter.GetTurnAngle(Vector3.forward, Vector3.forward);

            Assert.AreEqual(0f, angle, 0.1f);
        }

        [Test]
        public void GetTurnAngle_RightTurn_ReturnsPositive()
        {
            float angle = AnimationSpaceConverter.GetTurnAngle(Vector3.right, Vector3.forward);

            Assert.AreEqual(90f, angle, 0.1f);
        }

        [Test]
        public void GetTurnAngle_LeftTurn_ReturnsNegative()
        {
            float angle = AnimationSpaceConverter.GetTurnAngle(Vector3.left, Vector3.forward);

            Assert.AreEqual(-90f, angle, 0.1f);
        }

        [Test]
        public void GetTurnAngle_180Turn_Returns180OrMinus180()
        {
            float angle = AnimationSpaceConverter.GetTurnAngle(Vector3.back, Vector3.forward);

            Assert.AreEqual(180f, Mathf.Abs(angle), 0.1f);
        }

        [Test]
        public void GetTurnAngle_ZeroInput_ReturnsZero()
        {
            float angle = AnimationSpaceConverter.GetTurnAngle(Vector3.zero, Vector3.forward);

            Assert.AreEqual(0f, angle);
        }

        #endregion
    }
}
