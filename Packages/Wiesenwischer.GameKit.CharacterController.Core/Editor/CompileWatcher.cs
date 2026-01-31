using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Editor
{
    /// <summary>
    /// Überwacht Kompilierung und loggt Status, Warnungen und Fehler.
    /// </summary>
    [InitializeOnLoad]
    public static class CompileWatcher
    {
        private static readonly string LogPath = Path.Combine(
            Application.dataPath,
            "..",
            "Logs",
            "compile_errors.log"
        );

        private static int _totalAssemblies;
        private static int _totalErrors;
        private static int _totalWarnings;
        private static DateTime _compilationStartTime;
        private static StringBuilder _logBuilder;

        // Settings
        private const string PREF_LOG_TO_CONSOLE = "CompileWatcher_LogToConsole";
        private static bool LogToConsole
        {
            get => EditorPrefs.GetBool(PREF_LOG_TO_CONSOLE, true);
            set => EditorPrefs.SetBool(PREF_LOG_TO_CONSOLE, value);
        }

        static CompileWatcher()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

            // Log dass wir aktiv sind
            Debug.Log("[CompileWatcher] Initialized - watching for compilations");
        }

        private static void OnCompilationStarted(object context)
        {
            _totalAssemblies = 0;
            _totalErrors = 0;
            _totalWarnings = 0;
            _compilationStartTime = DateTime.Now;
            _logBuilder = new StringBuilder();

            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                AppendLog("╔════════════════════════════════════════════════════════════╗");
                AppendLog($"║  COMPILATION STARTED: {_compilationStartTime:yyyy-MM-dd HH:mm:ss}              ║");
                AppendLog("╚════════════════════════════════════════════════════════════╝");
                AppendLog("");

                if (LogToConsole)
                {
                    Debug.Log($"[CompileWatcher] ▶ Compilation started at {_compilationStartTime:HH:mm:ss}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CompileWatcher] Could not initialize log: {e.Message}");
            }
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            _totalAssemblies++;

            string assemblyName = Path.GetFileName(assemblyPath);

            int errors = 0;
            int warnings = 0;

            if (messages != null)
            {
                foreach (var msg in messages)
                {
                    if (msg.type == CompilerMessageType.Error) errors++;
                    else if (msg.type == CompilerMessageType.Warning) warnings++;
                }
            }

            _totalErrors += errors;
            _totalWarnings += warnings;

            // Status icon
            string status = errors > 0 ? "❌" : warnings > 0 ? "⚠️" : "✅";

            AppendLog($"─── {status} {assemblyName} ───");

            if (errors == 0 && warnings == 0)
            {
                AppendLog("  Compiled successfully (no issues)");
            }
            else
            {
                AppendLog($"  {errors} error(s), {warnings} warning(s)");
            }

            // Log to console
            if (LogToConsole)
            {
                if (errors > 0)
                {
                    Debug.LogError($"[CompileWatcher] ❌ {assemblyName}: {errors} error(s), {warnings} warning(s)");
                }
                else if (warnings > 0)
                {
                    Debug.LogWarning($"[CompileWatcher] ⚠️ {assemblyName}: {warnings} warning(s)");
                }
                else
                {
                    Debug.Log($"[CompileWatcher] ✅ {assemblyName}");
                }
            }

            // Log all messages (errors and warnings)
            if (messages != null && messages.Length > 0)
            {
                AppendLog("");
                foreach (var msg in messages)
                {
                    string prefix;
                    switch (msg.type)
                    {
                        case CompilerMessageType.Error:
                            prefix = "  [ERROR]";
                            break;
                        case CompilerMessageType.Warning:
                            prefix = "  [WARN] ";
                            break;
                        default:
                            prefix = "  [INFO] ";
                            break;
                    }

                    // Shorten file path for readability
                    string filePath = msg.file;
                    if (!string.IsNullOrEmpty(filePath) && filePath.Contains("Packages/"))
                    {
                        filePath = filePath.Substring(filePath.IndexOf("Packages/"));
                    }

                    AppendLog($"{prefix} {filePath}:{msg.line}");
                    AppendLog($"           {msg.message}");

                    // Also log to console
                    if (LogToConsole)
                    {
                        string consoleMsg = $"[CompileWatcher] {filePath}:{msg.line}\n{msg.message}";
                        if (msg.type == CompilerMessageType.Error)
                            Debug.LogError(consoleMsg);
                        else if (msg.type == CompilerMessageType.Warning)
                            Debug.LogWarning(consoleMsg);
                    }
                }
            }

            AppendLog("");
        }

        private static void OnCompilationFinished(object context)
        {
            var duration = DateTime.Now - _compilationStartTime;

            AppendLog("╔════════════════════════════════════════════════════════════╗");
            AppendLog($"║  COMPILATION FINISHED: {DateTime.Now:yyyy-MM-dd HH:mm:ss}             ║");
            AppendLog("╠════════════════════════════════════════════════════════════╣");
            AppendLog($"║  Duration:   {duration.TotalSeconds:F2}s                                       ║");
            AppendLog($"║  Assemblies: {_totalAssemblies,-5}                                        ║");
            AppendLog($"║  Errors:     {_totalErrors,-5}                                        ║");
            AppendLog($"║  Warnings:   {_totalWarnings,-5}                                        ║");
            AppendLog("╠════════════════════════════════════════════════════════════╣");

            string result;
            if (_totalErrors > 0)
            {
                result = "❌ FAILED";
                AppendLog("║  Result: ❌ FAILED                                         ║");
            }
            else if (_totalWarnings > 0)
            {
                result = "⚠️ SUCCESS (with warnings)";
                AppendLog("║  Result: ⚠️  SUCCESS (with warnings)                       ║");
            }
            else
            {
                result = "✅ SUCCESS";
                AppendLog("║  Result: ✅ SUCCESS                                        ║");
            }

            AppendLog("╚════════════════════════════════════════════════════════════╝");

            // Write to file
            try
            {
                File.WriteAllText(LogPath, _logBuilder.ToString());
            }
            catch { }

            // Log summary to console
            if (LogToConsole)
            {
                string summary = $"[CompileWatcher] ■ Compilation finished: {result} | {_totalAssemblies} assemblies | {duration.TotalSeconds:F2}s";
                if (_totalErrors > 0)
                    Debug.LogError(summary);
                else if (_totalWarnings > 0)
                    Debug.LogWarning(summary);
                else
                    Debug.Log(summary);
            }
        }

        private static void AppendLog(string line)
        {
            _logBuilder?.AppendLine(line);
        }

        #region Menu Items

        [MenuItem("Wiesenwischer/GameKit/Show Compile Log", false, 200)]
        public static void ShowCompileLog()
        {
            if (File.Exists(LogPath))
            {
                string content = File.ReadAllText(LogPath);
                Debug.Log($"[CompileWatcher] === COMPILE LOG ===\n{content}");

                // Also try to open in external editor
                try
                {
                    System.Diagnostics.Process.Start(LogPath);
                }
                catch
                {
                    Debug.Log($"[CompileWatcher] Log file: {LogPath}");
                }
            }
            else
            {
                Debug.Log("[CompileWatcher] No compile log found. Trigger a recompile first.");
            }
        }

        [MenuItem("Wiesenwischer/GameKit/Force Recompile", false, 201)]
        public static void ForceRecompile()
        {
            Debug.Log("[CompileWatcher] Requesting script compilation...");
            CompilationPipeline.RequestScriptCompilation();
        }

        [MenuItem("Wiesenwischer/GameKit/Toggle Console Logging", false, 202)]
        public static void ToggleConsoleLogging()
        {
            LogToConsole = !LogToConsole;
            Debug.Log($"[CompileWatcher] Console logging: {(LogToConsole ? "ON" : "OFF")}");
        }

        [MenuItem("Wiesenwischer/GameKit/Toggle Console Logging", true)]
        public static bool ToggleConsoleLoggingValidate()
        {
            Menu.SetChecked("Wiesenwischer/GameKit/Toggle Console Logging", LogToConsole);
            return true;
        }

        #endregion
    }
}
