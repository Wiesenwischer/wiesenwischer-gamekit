using NUnit.Framework;
using System.IO;
using System.Linq;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests
{
    /// <summary>
    /// Verifiziert dass kein Time.deltaTime/Time.time in Simulation-Code verwendet wird.
    /// Die Simulation muss deterministisch sein — deltaTime wird als Parameter durchgereicht.
    /// </summary>
    [TestFixture]
    public class DeterministicTimingTests
    {
        private static readonly string CoreRuntimePath = Path.GetFullPath(
            Path.Combine("Packages", "Wiesenwischer.GameKit.CharacterController.Core", "Runtime"));

        private static readonly string[] SimulationDirectories = new[]
        {
            Path.Combine(CoreRuntimePath, "Core", "StateMachine"),
            Path.Combine(CoreRuntimePath, "Core", "Locomotion"),
        };

        // Pfade die Time.deltaTime/Time.time verwenden duerfen (Rendering-Layer)
        private static readonly string[] AllowedDirectories = new[]
        {
            Path.Combine(CoreRuntimePath, "Camera"),
            Path.Combine(CoreRuntimePath, "Animation"),
            Path.Combine(CoreRuntimePath, "IK"),
            Path.Combine(CoreRuntimePath, "Core", "Visual"),
        };

        [Test]
        public void SimulationCode_NoTimeDeltaTime()
        {
            var violations = FindTimingViolations("Time.deltaTime");

            if (violations.Count > 0)
            {
                var message = "Time.deltaTime in Simulation-Code gefunden!\n" +
                    "Simulation muss deterministisch sein (deltaTime als Parameter).\n\n" +
                    string.Join("\n", violations.Select(v => $"  {v.File}:{v.Line} — {v.Content.Trim()}"));
                Assert.Fail(message);
            }
        }

        [Test]
        public void SimulationCode_NoTimeTime()
        {
            var violations = FindTimingViolations("Time.time");

            if (violations.Count > 0)
            {
                var message = "Time.time in Simulation-Code gefunden!\n" +
                    "Simulation muss tick-basiert sein (kein Zugriff auf Echtzeit).\n\n" +
                    string.Join("\n", violations.Select(v => $"  {v.File}:{v.Line} — {v.Content.Trim()}"));
                Assert.Fail(message);
            }
        }

        [Test]
        public void SimulationCode_NoTimeFixedDeltaTime()
        {
            var violations = FindTimingViolations("Time.fixedDeltaTime");

            if (violations.Count > 0)
            {
                var message = "Time.fixedDeltaTime in Simulation-Code gefunden!\n" +
                    "Simulation muss TickDelta verwenden (nicht Unity FixedDeltaTime).\n\n" +
                    string.Join("\n", violations.Select(v => $"  {v.File}:{v.Line} — {v.Content.Trim()}"));
                Assert.Fail(message);
            }
        }

        [Test]
        public void PlayerMovementState_UpdateSignature_TakesDeltaTime()
        {
            // Verifiziert dass die State-Basisklasse deltaTime als Parameter nimmt
            // und nicht intern Time.deltaTime verwendet
            var stateFile = Path.Combine(CoreRuntimePath, "Core", "StateMachine", "States", "PlayerMovementState.cs");

            if (!File.Exists(stateFile))
            {
                Assert.Inconclusive($"Datei nicht gefunden: {stateFile}");
                return;
            }

            var content = File.ReadAllText(stateFile);

            Assert.IsTrue(content.Contains("void Update(float deltaTime)"),
                "PlayerMovementState.Update muss deltaTime als Parameter akzeptieren");

            Assert.IsTrue(content.Contains("stateTime += deltaTime"),
                "stateTime muss mit uebergebenem deltaTime akkumuliert werden");

            Assert.IsFalse(content.Contains("Time.deltaTime"),
                "PlayerMovementState darf NICHT Time.deltaTime verwenden");
        }

        [Test]
        public void StateMachine_UpdateSignature_TakesDeltaTime()
        {
            var smFile = Path.Combine(CoreRuntimePath, "Core", "StateMachine", "PlayerMovementStateMachine.cs");

            if (!File.Exists(smFile))
            {
                Assert.Inconclusive($"Datei nicht gefunden: {smFile}");
                return;
            }

            var content = File.ReadAllText(smFile);

            Assert.IsTrue(content.Contains("void Update(float deltaTime)"),
                "StateMachine.Update muss deltaTime als Parameter akzeptieren");

            Assert.IsTrue(content.Contains("void PhysicsUpdate(float deltaTime)"),
                "StateMachine.PhysicsUpdate muss deltaTime als Parameter akzeptieren");
        }

        private struct Violation
        {
            public string File;
            public int Line;
            public string Content;
        }

        private System.Collections.Generic.List<Violation> FindTimingViolations(string pattern)
        {
            var violations = new System.Collections.Generic.List<Violation>();

            foreach (var dir in SimulationDirectories)
            {
                if (!Directory.Exists(dir)) continue;

                var files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    // Ueberspringe erlaubte Pfade
                    if (AllowedDirectories.Any(allowed => file.StartsWith(allowed))) continue;

                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];

                        // Ueberspringe Kommentare
                        var trimmed = line.TrimStart();
                        if (trimmed.StartsWith("//") || trimmed.StartsWith("*") || trimmed.StartsWith("///"))
                            continue;

                        if (line.Contains(pattern))
                        {
                            violations.Add(new Violation
                            {
                                File = Path.GetFileName(file),
                                Line = i + 1,
                                Content = line
                            });
                        }
                    }
                }
            }

            return violations;
        }
    }
}
