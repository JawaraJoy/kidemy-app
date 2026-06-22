#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using UnityEngine;

public class BatchScenesBuilder
{
    [MenuItem("Build/Build Scenes To HTML5")]
    public static void BuildAllScenesToSeparateFolders()
    {
        // Get all scenes in the build settings (or replace this to target a specific folder)
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        
        if (scenes.Length == 0)
        {
            Debug.LogError("No scenes found in Build Settings! Please add them first.");
            return;
        }

        // Define base output directory
        string baseOutputPath = Path.Combine(Application.dataPath, "../Builds/HTML5");

        foreach (EditorBuildSettingsScene buildScene in scenes)
        {
            if (!buildScene.enabled) continue;

            // Extract the scene name
            string sceneName = Path.GetFileNameWithoutExtension(buildScene.path);

            if(sceneName.IndexOf("Dev_001") < 0) continue;
            
            // Define the path to build specifically for this scene
            string sceneOutputPath = Path.Combine(baseOutputPath, sceneName);

            // Setup build player options
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { buildScene.path };
            buildPlayerOptions.locationPathName = sceneOutputPath;
            buildPlayerOptions.target = BuildTarget.WebGL;
            buildPlayerOptions.options = BuildOptions.None;

            Debug.Log($"Building scene '{sceneName}' to {sceneOutputPath}...");

            // Perform the build
            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }

        Debug.Log("All scene builds completed!");
    }
}
#endif