using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Prediction;

#pragma warning disable CS0612 // Obsolete types — Tests fuer Legacy-Prediction-Code

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests
{
    [TestFixture]
    public class ReconciliationTests
    {
        private PredictionBuffer _buffer;

        [SetUp]
        public void SetUp()
        {
            _buffer = new PredictionBuffer(capacity: 128);
        }

        [Test]
        public void ValidateAgainstServer_ReturnsTrue_WhenMatching()
        {
            var localState = PredictionState.Create(
                tick: 10,
                position: new Vector3(5f, 0f, 5f),
                rotation: 90f,
                velocity: Vector3.forward,
                stateName: "Grounded",
                isGrounded: true);

            _buffer.Add(localState);

            var serverState = PredictionState.Create(
                tick: 10,
                position: new Vector3(5.01f, 0f, 5.01f),
                rotation: 90f,
                velocity: Vector3.forward,
                stateName: "Grounded",
                isGrounded: true);

            Assert.IsTrue(_buffer.ValidateAgainstServer(
                serverState, positionThreshold: 0.1f));
        }

        [Test]
        public void ValidateAgainstServer_ReturnsFalse_WhenMismatch()
        {
            var localState = PredictionState.Create(
                tick: 10,
                position: new Vector3(5f, 0f, 5f),
                rotation: 90f,
                velocity: Vector3.forward,
                stateName: "Grounded",
                isGrounded: true);

            _buffer.Add(localState);

            var serverState = PredictionState.Create(
                tick: 10,
                position: new Vector3(6f, 0f, 6f),
                rotation: 90f,
                velocity: Vector3.forward,
                stateName: "Grounded",
                isGrounded: true);

            Assert.IsFalse(_buffer.ValidateAgainstServer(
                serverState, positionThreshold: 0.1f));
        }

        [Test]
        public void RemoveAfter_ClearsStatesForRollback()
        {
            for (int i = 0; i < 20; i++)
            {
                _buffer.Add(PredictionState.Create(
                    tick: i,
                    position: Vector3.zero,
                    rotation: 0f,
                    velocity: Vector3.zero,
                    stateName: "Grounded",
                    isGrounded: true));
            }

            _buffer.RemoveAfter(10);

            Assert.IsTrue(_buffer.TryGet(10, out _));
            Assert.IsFalse(_buffer.TryGet(11, out _));
            Assert.IsFalse(_buffer.TryGet(19, out _));
        }

        [Test]
        public void GetFromTick_ReturnsStatesForResimulation()
        {
            for (int i = 0; i < 10; i++)
            {
                _buffer.Add(PredictionState.Create(
                    tick: i,
                    position: Vector3.one * i,
                    rotation: 0f,
                    velocity: Vector3.zero,
                    stateName: "Grounded",
                    isGrounded: true));
            }

            var states = _buffer.GetFromTick(5);
            Assert.AreEqual(5, states.Count);
        }
    }
}
