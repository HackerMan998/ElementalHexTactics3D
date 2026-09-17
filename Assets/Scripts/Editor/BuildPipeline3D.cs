#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ElementalHexTactics3D.Editor
{
    /// <summary>
    /// 1-Click Standalone Build automation for publisher demo submissions (TGFI, IGDX, Steam playtests).
    /// </summary>
    public static class BuildPipeline3D
    {
        [MenuItem("Elemental Hex 3D/Build Standalone Demo (Windows .exe)", false, 20)]
        public static void BuildWindowsDemo()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string buildFolder = Path.Combine(projectRoot, "Builds", "Windows");

            if (!Directory.Exists(buildFolder))
            {
                Directory.CreateDirectory(buildFolder);
            }

            string exePath = Path.Combine(buildFolder, "ElementalHexTactics3D.exe");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"<color=#00E5FF><b>[Build Pipeline]</b></color> Compiling standalone demo build to: {exePath}...");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                long sizeMb = (long)(summary.totalSize / (1024 * 1024));
                Debug.Log($"<color=#4CAF50><b>[Build Pipeline] SUCCESS!</b></color> Demo build created ({sizeMb} MB) in: {buildFolder}");
                EditorUtility.RevealInFinder(exePath);
            }
            else
            {
                Debug.LogError($"<color=#EF5350><b>[Build Pipeline] FAILED</b></color> Build result: {summary.result}");
            }
        }
    }
}
#endif

