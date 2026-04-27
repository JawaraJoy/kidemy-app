#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class BatchScenesBuilder
{
    [MenuItem("Build/Build WebGL Scenes")]
    public static void BuildAllScenes()
    {
        // 1. Define target folders
        string[] scenes = {
            "Assets/_dev/Scenes/Dev_001_FrogsCount_A.unity",
            "Assets/_dev/Scenes/Dev_001_FrogsCount_B.unity",
            "Assets/_dev/Scenes/Dev_001_FrogsCount_C.unity",
            "Assets/_dev/Scenes/Dev_002_AdditionTrek_A.unity",
            "Assets/_dev/Scenes/Dev_002_AdditionTrek_B.unity",
            "Assets/_dev/Scenes/Dev_002_AdditionTrek_C.unity",
            "Assets/_dev/Scenes/Dev_003_PatternJungle_A.unity",
            "Assets/_dev/Scenes/Dev_003_PatternJungle_B.unity",
            "Assets/_dev/Scenes/Dev_003_PatternJungle_C.unity",
            "Assets/_dev/Scenes/Dev_004_PuzzleJungle_A.unity",
            "Assets/_dev/Scenes/Dev_004_PuzzleJungle_B.unity",
            "Assets/_dev/Scenes/Dev_004_PuzzleJungle_C.unity",
            "Assets/_dev/Scenes/Dev_005_SoundQuest_A.unity",
            "Assets/_dev/Scenes/Dev_005_SoundQuest_B.unity",
            "Assets/_dev/Scenes/Dev_005_SoundQuest_C.unity",
            "Assets/_dev/Scenes/Dev_006_LetterHunt_A.unity",
            "Assets/_dev/Scenes/Dev_006_LetterHunt_B.unity",
            "Assets/_dev/Scenes/Dev_006_LetterHunt_C.unity",
            "Assets/_dev/Scenes/Dev_007_WordBuilder_A.unity",
            "Assets/_dev/Scenes/Dev_007_WordBuilder_B.unity",
            "Assets/_dev/Scenes/Dev_007_WordBuilder_C.unity",
            "Assets/_dev/Scenes/Dev_008_StoryAdventure_A.unity",
            "Assets/_dev/Scenes/Dev_008_StoryAdventure_B.unity",
            "Assets/_dev/Scenes/Dev_008_StoryAdventure_C.unity",
        };
        
        string baseBuildPath = "Builds/";

        // 2. Setup BuildPlayer Options
        for (int i = 0; i < scenes.Length; i++)
        {
            string sceneName = Path.GetFileNameWithoutExtension(scenes[i]);
            string buildPath = baseBuildPath + sceneName;

            // Create folder if it doesn't exist
            if (!Directory.Exists(buildPath))
                Directory.CreateDirectory(buildPath);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { scenes[i] };
            buildPlayerOptions.locationPathName = buildPath;
            buildPlayerOptions.target = BuildTarget.WebGL;
            buildPlayerOptions.options = BuildOptions.None;

            // 3. Build the scene
            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }
    }
}
#endif