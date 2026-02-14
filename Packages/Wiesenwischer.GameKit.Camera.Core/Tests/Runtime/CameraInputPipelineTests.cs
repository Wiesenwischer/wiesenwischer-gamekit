using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Tests
{
    [TestFixture]
    public class CameraInputPipelineTests
    {
        private GameObject _pipelineGO;
        private CameraInputPipeline _pipeline;

        [SetUp]
        public void SetUp()
        {
            _pipelineGO = new GameObject("InputPipeline");
            _pipeline = _pipelineGO.AddComponent<CameraInputPipeline>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_pipelineGO);
        }

        [Test]
        public void ProcessInput_WithoutActions_ReturnsZero()
        {
            var result = _pipeline.ProcessInput(1f / 60f);

            Assert.AreEqual(0f, result.LookX);
            Assert.AreEqual(0f, result.LookY);
            Assert.AreEqual(0f, result.Zoom);
        }

        [Test]
        public void CurrentInput_DefaultIsZero()
        {
            var input = _pipeline.CurrentInput;

            Assert.AreEqual(0f, input.LookX);
            Assert.AreEqual(0f, input.LookY);
            Assert.AreEqual(0f, input.Zoom);
            Assert.IsFalse(input.IsGamepad);
        }
    }
}
