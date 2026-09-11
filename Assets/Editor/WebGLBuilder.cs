using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuilder
{
    public static void Build()
    {
        string output = Path.Combine(
            Directory.GetCurrentDirectory(),
            new string(new[] { 'B', 'u', 'i', 'l', 'd', 's', '/', 'W', 'e', 'b' }));
        Directory.CreateDirectory(output);

        var scenes = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled) scenes.Add(scene.path);
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError(report.summary.result);
            EditorApplication.Exit(1);
        }
    }
}
