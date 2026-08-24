using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DiamondTilt.Core.EditorTools
{
    internal static class BuildScript
    {
        private const string OutputPath = "Builds/macOS/DiamondTilt.app";

        [MenuItem("Diamond Tilt/Build macOS")]
        internal static void BuildMacOSFromMenu() => BuildMacOS();

        internal static void BuildMacOS()
        {
            Directory.CreateDirectory("Builds/macOS");

            var options = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/Scenes/Boot.unity",
                    "Assets/Scenes/Match.unity",
                },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception($"Build failed: {report.summary.result}");

            Debug.Log($"[DiamondTilt] Build succeeded -> {OutputPath} ({report.summary.totalSize / 1048576} MB)");
        }
    }
}
