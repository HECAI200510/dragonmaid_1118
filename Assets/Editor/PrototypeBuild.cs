using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PrototypeBuild
{
    private const string MainScene = "Assets/Scenes/NightWatch.unity";

    [MenuItem("Build/Build Windows Prototype")]
    public static void BuildWindows()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string buildDirectory = Path.Combine(root, "Builds", "Windows");
        Directory.CreateDirectory(buildDirectory);

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { MainScene },
            locationPathName = Path.Combine(buildDirectory, "DragonMaidSleepClock.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.StrictMode
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException(
                $"Windows build {report.summary.result} with {report.summary.totalErrors} errors.");
        }

        string notices = Path.Combine(buildDirectory, "ThirdParty", "NotoSansSC");
        Directory.CreateDirectory(notices);
        File.Copy(Path.Combine(root, "ThirdParty", "NotoSansSC", "OFL.txt"),
            Path.Combine(notices, "OFL.txt"), true);
        Debug.Log($"Windows prototype ready at {report.summary.outputPath}");
    }
}
