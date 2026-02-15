using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Tests
{
    [TestFixture]
    public class CameraPresetTests
    {
        private CameraPreset _preset;

        [SetUp]
        public void SetUp()
        {
            _preset = ScriptableObject.CreateInstance<CameraPreset>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_preset);
        }

        [Test]
        public void CameraPreset_HasDefaultValues()
        {
            Assert.AreEqual(60f, _preset.DefaultFov, 0.001f);
            Assert.AreEqual(-40f, _preset.PitchMin, 0.001f);
            Assert.AreEqual(70f, _preset.PitchMax, 0.001f);
            Assert.AreEqual(5f, _preset.DefaultDistance, 0.001f);
            Assert.AreEqual(2f, _preset.MinDistance, 0.001f);
            Assert.AreEqual(15f, _preset.MaxDistance, 0.001f);
        }

        [Test]
        public void CameraPreset_BoolDefaults()
        {
            Assert.IsTrue(_preset.InertiaEnabled);
            Assert.IsTrue(_preset.RecenterEnabled);
            Assert.IsFalse(_preset.ShoulderEnabled);
            Assert.IsFalse(_preset.DynamicOrbitEnabled);
            Assert.IsFalse(_preset.SoftTargetingEnabled);
        }

        [Test]
        public void SetPreset_NullPreset_DoesNotThrow()
        {
            var go = new GameObject("BrainTest");
            go.AddComponent<PivotRig>();
            var brain = go.AddComponent<CameraBrain>();

            Assert.DoesNotThrow(() => brain.SetPreset(null));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CameraPreset_FieldsAreModifiable()
        {
            _preset.DefaultFov = 55f;
            _preset.PitchMin = -50f;
            _preset.InertiaEnabled = false;

            Assert.AreEqual(55f, _preset.DefaultFov, 0.001f);
            Assert.AreEqual(-50f, _preset.PitchMin, 0.001f);
            Assert.IsFalse(_preset.InertiaEnabled);
        }

        [Test]
        public void CameraPreset_Description()
        {
            _preset.Description = "Test Preset";
            Assert.AreEqual("Test Preset", _preset.Description);
        }
    }
}
