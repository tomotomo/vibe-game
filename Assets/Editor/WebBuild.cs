using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class WebBuild
{
    [MenuItem("Build/Build WebGL")]
    public static void Build()
    {
        string buildPath = "Builds/WebGL";
        
        // Ensure build directory exists
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }

        // Explicitly set the MainScene for this project
        string[] scenes = { "Assets/Scenes/MainScene.unity" };

        // Configure Player Settings for WebGL
        PlayerSettings.runInBackground = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.defaultWebScreenWidth = 540;
        PlayerSettings.defaultWebScreenHeight = 960;

        // Build Player
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("WebGL Build succeeded: " + summary.totalSize + " bytes");
        }

        if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("WebGL Build failed");
            // Throw exception to ensure non-zero exit code for CI/CLI
            throw new System.Exception("Build failed");
        }
    }

    [MenuItem("Build/Build for GitHub Pages")]
    public static void BuildAndDeploy()
    {
        // 1. Build
        Build();

        string sourceDir = "Builds/WebGL";
        string targetDir = "docs";

        // 2. Clear docs directory
        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, true);
        }
        Directory.CreateDirectory(targetDir);

        // 3. Copy files
        CopyDirectory(sourceDir, targetDir);

        // 4. Create .nojekyll
        File.Create(Path.Combine(targetDir, ".nojekyll")).Dispose();

        Debug.Log($"Deployed WebGL build to {targetDir} for GitHub Pages.");

        EditorUtility.DisplayDialog("Build & Deploy Complete",
            "WebGL build deployed to 'docs/' successfully.\n\n" +
            "Next Steps for Gemini CLI:\n" +
            "Please return to the CLI and type:\n" +
            "\"ビルド完了、コミットして\"", 
            "OK");
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        DirectoryInfo[] dirs = dir.GetDirectories();

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(targetDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            string newTargetDir = Path.Combine(targetDir, subDir.Name);
            Directory.CreateDirectory(newTargetDir);
            CopyDirectory(subDir.FullName, newTargetDir);
        }
    }
}