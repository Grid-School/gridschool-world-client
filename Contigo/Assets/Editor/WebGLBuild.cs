using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// The WebGL build, from the terminal, with no hands on the editor:
///
///   Unity -batchmode -quit -projectPath Contigo -buildTarget WebGL \
///         -executeMethod WebGLBuild.Build -logFile -
///
/// Output lands in ../web (the folder the Pages workflow publishes), so a
/// build followed by a commit of web/ is a deploy. The staging and live
/// surfaces are static hosts with no control over Content-Encoding, so the
/// build ships gzip with Unity's decompression fallback: the loader inflates
/// in the browser and the files stay well under a static host's size caps.
/// Only the client scene is built; the server scene listed in EditorBuildSettings
/// no longer exists on disk and would fail the build.
/// </summary>
public static class WebGLBuild
{
    private const string Scene = "Assets/Scenes/MainClientScene.unity";
    private const long SizeCap = 100L * 1024 * 1024; // GitHub Pages: no single file over 100 MB

    public static void Build()
    {
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "web"));
        if (!File.Exists(Path.Combine(Application.dataPath, "..", Scene)))
            throw new Exception($"scene missing: {Scene}");

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.template = "PROJECT:GridSchool";

        var options = new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"WebGL build {report.summary.result}: {report.summary.totalErrors} errors");

        var oversize = new DirectoryInfo(output)
            .GetFiles("*", SearchOption.AllDirectories)
            .Where(f => f.Length > SizeCap)
            .Select(f => $"{f.Name} {f.Length / (1024 * 1024)} MB")
            .ToArray();
        if (oversize.Length > 0)
            throw new Exception("files too large for the static host: " + string.Join(", ", oversize));

        Debug.Log($"[WebGLBuild] ok → {output} ({report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalTime.TotalMinutes:F1} min)");
    }
}
