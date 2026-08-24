using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiamondTilt.Core.EditorTools
{
    internal static class DiamondTiltBootstrapper
    {
        private const string MenuRoot = "Diamond Tilt/";

        [MenuItem(MenuRoot + "Bootstrap Project")]
        internal static void Bootstrap()
        {
            EnsureAsmdefs();
            AssetDatabase.Refresh();
            ApplyPlayerSettings();
            EnsureScenes();
            Debug.Log("[DiamondTilt] Bootstrap complete. Open Scenes/Boot.unity and press Play.");
        }

        private static void EnsureAsmdefs()
        {
            AsmdefWriter.WriteIfMissing(
                "Assets/Scripts/Core/DiamondTilt.Core.asmdef",
                Json(true, "DiamondTilt.Core", Array.Empty<string>(), Array.Empty<string>()));

            AsmdefWriter.WriteIfMissing(
                "Assets/Scripts/Presentation/DiamondTilt.Presentation.asmdef",
                Json(false, "DiamondTilt.Presentation",
                    new[] { "DiamondTilt.Core" }, Array.Empty<string>()));

            AsmdefWriter.WriteIfMissing(
                "Assets/Scripts/Editor/DiamondTilt.EditorTools.asmdef",
                Json(false, "DiamondTilt.EditorTools",
                    new[] { "DiamondTilt.Core", "DiamondTilt.Presentation" }, Array.Empty<string>(),
                    editorOnly: true));

            AsmdefWriter.WriteIfMissing(
                "Assets/Tests/EditMode/DiamondTilt.Tests.EditMode.asmdef",
                Json(false, "DiamondTilt.Tests.EditMode",
                    new[] { "DiamondTilt.Core", "UnityEngine.TestRunner", "UnityEditor.TestRunner" },
                    new[] { "nunit.framework.dll" },
                    editorOnly: true, defineConstraints: new[] { "UNITY_INCLUDE_TESTS" },
                    overrideReferences: true));
        }

        private static string Json(bool noEngineRefs, string name, string[] references, string[] precompiled, bool editorOnly = false, string[] defineConstraints = null, bool overrideReferences = false)
        {
            var refs = "\"references\": [" + string.Join(",", Wrap(references)) + "]";
            var pre = precompiled.Length == 0 ? "" : ", \"precompiledReferences\": [" + string.Join(",", Wrap(precompiled)) + "]";
            var platforms = editorOnly ? ", \"includePlatforms\": [\"Editor\"]" : "";
            var constraints = defineConstraints is { Length: > 0 } ? ", \"defineConstraints\": [" + string.Join(",", Wrap(defineConstraints)) + "]" : "";
            var ovr = overrideReferences ? ", \"overrideReferences\": true" : "";
            var engine = noEngineRefs ? ", \"noEngineReferences\": true" : "";
            return "{\n  \"name\": \"" + name + "\",\n  \"rootNamespace\": \"DiamondTilt\",\n  "
                   + refs + pre + ovr + platforms + constraints + engine +
                   "\n}";
        }

        private static string[] Wrap(string[] values)
        {
            var result = new string[values.Length];
            for (int i = 0; i < values.Length; i++) result[i] = "\"" + values[i] + "\"";
            return result;
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "DiamondTilt";
            PlayerSettings.productName = "Diamond Tilt";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.diamondtilt.game");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.diamondtilt.game");
        }

        private static void EnsureScenes()
        {
            Directory.CreateDirectory("Assets/Scenes");
            EnsureScene("Assets/Scenes/Boot.unity", withRunner: true, playController: true);
            EnsureScene("Assets/Scenes/Match.unity", withRunner: false, autoPlayer: true);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Boot.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Match.unity", true),
            };
        }

        private static void EnsureScene(string path, bool withRunner, bool autoPlayer = false, bool playController = false)
        {
            if (File.Exists(path)) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (withRunner)
            {
                var runnerGo = new GameObject("GameRunner");
                runnerGo.AddComponent<DiamondTilt.Presentation.GameRunner>();
            }
            if (autoPlayer)
            {
                var playerGo = new GameObject("MatchAutoPlayer");
                playerGo.AddComponent<DiamondTilt.Presentation.MatchAutoPlayer>();
            }
            if (playController)
            {
                var controllerGo = new GameObject("MatchPlayController");
                controllerGo.AddComponent<DiamondTilt.Presentation.MatchPlayController>();
            }
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void EnsureScene(string path)
        {
            if (File.Exists(path)) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var runnerGo = new GameObject("GameRunner");
            runnerGo.AddComponent<DiamondTilt.Presentation.GameRunner>();
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
