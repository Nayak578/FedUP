using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FedUp.EditorTools
{
    /// <summary>One-click Windows builds into &lt;repo&gt;/builds/slice_N/FedUp.exe.</summary>
    public static class SliceBuilder
    {
        public const string GreyboxScene = "Assets/_Project/Scenes/Greybox.unity";

        public static string RepoRoot => Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;

        [MenuItem("FedUp/Build Current Slice (Windows)")]
        public static void BuildFromMenu() => Debug.Log(Build(0));

        public static string Build(int slice)
        {
            ApplyPlayerSettings();
            string dir = Path.Combine(RepoRoot, "builds", $"slice_{slice}");
            Directory.CreateDirectory(dir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { GreyboxScene },
                locationPathName = Path.Combine(dir, "FedUp.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            return $"BUILD {s.result} errors={s.totalErrors} warnings={s.totalWarnings} " +
                   $"size={s.totalSize / (1024 * 1024)}MB time={s.totalTime.TotalSeconds:F0}s path={options.locationPathName}";
        }

        public static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = "Fed Up";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.usePlayerLog = true;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GreyboxScene, true) };
        }
    }
}
