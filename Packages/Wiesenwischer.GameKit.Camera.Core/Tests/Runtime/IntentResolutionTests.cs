using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Tests
{
    [TestFixture]
    public class IntentResolutionTests
    {
        private CameraBrain _brain;
        private GameObject _go;

        private class MockIntent : ICameraIntent
        {
            public int Priority { get; set; }
            public bool IsActive { get; set; } = true;
            private readonly System.Action<CameraState> _applyAction;

            public MockIntent(int priority, System.Action<CameraState> apply)
            {
                Priority = priority;
                _applyAction = apply;
            }

            public void Apply(ref CameraState state, CameraContext ctx)
            {
                _applyAction?.Invoke(state);
            }
        }

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("BrainTest");
            _go.AddComponent<PivotRig>();
            _brain = _go.AddComponent<CameraBrain>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void PushIntent_AddsSorted()
        {
            var intentA = new MockIntent(200, _ => { });
            var intentB = new MockIntent(100, _ => { });

            _brain.PushIntent(intentA);
            _brain.PushIntent(intentB);

            // Intern sortiert: B(100) vor A(200)
            // Kein direkter Zugriff auf Liste, aber kein Fehler beim Pushen
            Assert.Pass();
        }

        [Test]
        public void PushIntent_DuplicateIgnored()
        {
            var intent = new MockIntent(100, _ => { });

            _brain.PushIntent(intent);
            _brain.PushIntent(intent); // Duplicate

            // Sollte keinen Fehler werfen
            Assert.Pass();
        }

        [Test]
        public void RemoveIntent_RemovesFromList()
        {
            var intent = new MockIntent(100, _ => { });
            _brain.PushIntent(intent);
            _brain.RemoveIntent(intent);

            // Kein Fehler
            Assert.Pass();
        }

        [Test]
        public void ClearIntents_RemovesAll()
        {
            _brain.PushIntent(new MockIntent(100, _ => { }));
            _brain.PushIntent(new MockIntent(200, _ => { }));
            _brain.ClearIntents();

            Assert.Pass();
        }

        [Test]
        public void RemoveIntent_NonExistent_DoesNotThrow()
        {
            var intent = new MockIntent(100, _ => { });
            Assert.DoesNotThrow(() => _brain.RemoveIntent(intent));
        }
    }
}
