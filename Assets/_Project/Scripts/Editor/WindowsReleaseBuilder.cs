using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DustlineArena.Editor
{
    public static class WindowsReleaseBuilder
    {
        [MenuItem("Dustline Arena/Build Windows x64 Release")]
        public static void Build()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            string[] campaign = new[] { "Dustline_MainMenu" }
                .Concat(Enumerable.Range(1, 5).Select(index => $"Dustline_Arena_{index:00}"))
                .ToArray();

            if (!scenes.Select(Path.GetFileNameWithoutExtension).SequenceEqual(campaign))
            {
                throw new InvalidOperationException("Enable the main menu and all five arenas in campaign order in Build Settings.");
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string buildDirectory = Path.Combine(projectRoot, "Builds", "Windows-x64");
            Directory.CreateDirectory(buildDirectory);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(buildDirectory, "DustlineArena.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.StrictMode | BuildOptions.CompressWithLz4HC
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows build {report.summary.result}: {report.summary.totalErrors} errors.");
            }

            Debug.Log($"Dustline Arena Windows build succeeded: {report.summary.outputPath}");
        }
    }
}
