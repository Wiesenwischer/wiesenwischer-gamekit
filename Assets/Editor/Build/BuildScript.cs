using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using System;

/// <summary>
/// Build-Script fuer Client- und Server-Builds.
/// Aufruf via CLI: Unity -executeMethod BuildScript.BuildLinuxServer
/// </summary>
public static class BuildScript
{
    private static readonly string[] GameScenes =
    {
        "Assets/Scenes/Playground.unity"
    };

    /// <summary>
    /// Baut den Linux Dedicated Server (fuer Docker).
    /// Aufruf: -executeMethod BuildScript.BuildLinuxServer
    /// </summary>
    public static void BuildLinuxServer()
    {
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

        // HDRP erfordert Vulkan auf Linux (OpenGLCore nicht unterstuetzt)
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64,
            new[] { GraphicsDeviceType.Vulkan });

        var options = new BuildPlayerOptions
        {
            scenes = GameScenes,
            locationPathName = "Builds/Server/GameKit_HDRP",
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None
        };

        Build(options);
    }

    /// <summary>
    /// Baut den Windows Client.
    /// Aufruf: -executeMethod BuildScript.BuildWindowsClient
    /// </summary>
    public static void BuildWindowsClient()
    {
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;

        var options = new BuildPlayerOptions
        {
            scenes = GameScenes,
            locationPathName = "Builds/Client/GameKit_HDRP.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Build(options);
    }

    private static void Build(BuildPlayerOptions options)
    {
        Debug.Log($"[BuildScript] Starte Build: {options.target} → {options.locationPathName}");

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        Debug.Log($"[BuildScript] Result: {summary.result} | Errors: {summary.totalErrors} | Warnings: {summary.totalWarnings} | Time: {summary.totalTime}");

        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[BuildScript] Build fehlgeschlagen: {summary.totalErrors} Fehler");
            EditorApplication.Exit(1);
        }

        Debug.Log($"[BuildScript] Build erfolgreich: {summary.outputPath}");
    }
}
