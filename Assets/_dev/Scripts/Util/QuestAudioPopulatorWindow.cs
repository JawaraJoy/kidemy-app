#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor; // Package for Edit-Mode Coroutines
using EduGame; // Matches your project namespace

public class QuestAudioPopulatorWindow : EditorWindow
{
    private const string TARGET_PREFAB_FOLDER = "Assets/_dev/Prefabs/Quest/Templates";
    private bool forceRedownload = false;
    private bool isProcessing = false;

    [MenuItem("Tools/Audio/Populate Quest Audio")]
    public static void ShowWindow()
    {
        GetWindow<QuestAudioPopulatorWindow>("Quest Audio Populator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Quest Prefab Audio Batch Populator", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox($"Target Prefab Folder:\n{TARGET_PREFAB_FOLDER}", MessageType.Info);

        forceRedownload = EditorGUILayout.Toggle("Force Redownload Existing", forceRedownload);

        EditorGUILayout.Space(10);

        GUI.enabled = !isProcessing;
        if (GUILayout.Button("Scan Prefabs & Populate Audio", GUILayout.Height(35)))
        {
            // Start coroutine cleanly in Edit Mode without MonoBehaviour!
            EditorCoroutineUtility.StartCoroutineOwnerless(Routine_PopulateAllQuests());
        }
        GUI.enabled = true;
    }

    private APIManager FindAPIManagerInScene()
    {
        // Find the active APIManager instance in the current scene
        APIManager apiManager = Object.FindAnyObjectByType<APIManager>();

        if (apiManager == null)
        {
            Debug.LogError("[QuestAudioPopulator] APIManager GameObject was not found in the active scene! Please open a scene containing APIManager.");
        }

        return apiManager;
    }

    private IEnumerator Routine_PopulateAllQuests()
    {
        // 1. Ensure APIManager exists in the active scene
        APIManager apiManager = Object.FindAnyObjectByType<APIManager>();
        if (apiManager == null)
        {
            Debug.LogError("[QuestAudioPopulator] APIManager GameObject was not found in the active scene! Please open a scene containing APIManager.");
            yield break;
        }

        isProcessing = true;

        if (!Directory.Exists(TARGET_PREFAB_FOLDER))
        {
            Debug.LogError($"[QuestAudioPopulator] Folder missing: {TARGET_PREFAB_FOLDER}");
            isProcessing = false;
            yield break;
        }

        // 2. Scan for all prefabs in the target directory
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { TARGET_PREFAB_FOLDER });
        Debug.Log($"[QuestAudioPopulator] Found {guids.Length} prefabs in {TARGET_PREFAB_FOLDER}");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefabGO == null) continue;

            // 3. Retrieve Quest component (or child classes)
            Quest questComponent = prefabGO.GetComponent<Quest>();
            if (questComponent == null) continue;

            if (questComponent.Data == null)
            {
                Debug.LogWarning($"[QuestAudioPopulator] Prefab '{prefabGO.name}' has no SO_Quest 'data' assigned. Skipping.");
                continue;
            }

            // 4. Get VoiceRequests (Question text + Voice ID from linked SO_Character)
            VoiceRequest[] requests = questComponent.GetVoiceRequests();
            if (requests == null || requests.Length == 0) continue;

            bool isSOModified = false;
            SO_Quest targetSO = questComponent.Data;

            foreach (var req in requests)
            {
                // Skip existing audio clips unless Force Redownload is checked
                if (!forceRedownload && questComponent.HasVoiceClip(req.id))
                {
                    Debug.Log($"[QuestAudioPopulator] Skipping '{prefabGO.name}' -> ID '{req.id}' (Audio exists)");
                    continue;
                }

                if (string.IsNullOrEmpty(req.text)) continue;

                Debug.Log($"[QuestAudioPopulator] Requesting Audio for '{prefabGO.name}' | VoiceID: '{req.voice_id}' | ID: '{req.id}'");

                bool requestComplete = false;
                string audioUrl = null;

                // 5. Call APIManager to get the JSON response containing audio_url
                apiManager.RequestVoice(req,
                    onSuccess: (jsonResponse) =>
                    {
                        try
                        {
                            // Using your EduGame.VoiceResult class!
                            EduGame.VoiceResult result = JsonUtility.FromJson<EduGame.VoiceResult>(jsonResponse);
                            audioUrl = result != null ? result.audio_url : null;
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[QuestAudioPopulator] Failed to parse JSON response: {ex.Message}");
                        }

                        requestComplete = true;
                    },
                    onFailure: (error) =>
                    {
                        Debug.LogError($"[QuestAudioPopulator] API Error for '{req.id}': {error}");
                        requestComplete = true;
                    }
                );

                // Wait until APIManager returns the JSON
                yield return new WaitUntil(() => requestComplete);

                if (string.IsNullOrEmpty(audioUrl))
                {
                    Debug.LogWarning($"[QuestAudioPopulator] Skipping '{req.id}' due to missing audio_url.");
                    continue;
                }

                // 6. Download the actual MP3 file bytes from audioUrl
                using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(audioUrl))
                {
                    yield return www.SendWebRequest();

                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        byte[] audioBytes = www.downloadHandler.data;

                        // Folder path: Assets/_dev/Audio/Voices/[SO_Name]/
                        string relativeFolder = $"Assets/_dev/Audio/Voices/{targetSO.name}";
                        string fullFolder = Path.Combine(Application.dataPath, $"_dev/Audio/Voices/{targetSO.name}");

                        if (!Directory.Exists(fullFolder))
                        {
                            Directory.CreateDirectory(fullFolder);
                        }

                        string relativeFilePath = $"{relativeFolder}/{req.id}.mp3";
                        string fullFilePath = Path.Combine(fullFolder, $"{req.id}.mp3");

                        // Write file to disk and refresh AssetDatabase
                        File.WriteAllBytes(fullFilePath, audioBytes);
                        AssetDatabase.Refresh();

                        // Load imported AudioClip and assign to ScriptableObject
                        AudioClip importedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativeFilePath);
                        if (importedClip != null)
                        {
                            questComponent.AssignVoiceClip(req.id, importedClip);
                            isSOModified = true;
                            Debug.Log($"[QuestAudioPopulator] Successfully assigned '{req.id}' to '{targetSO.name}'!");
                        }
                        else
                        {
                            Debug.LogError($"[QuestAudioPopulator] Failed to load imported AudioClip at: {relativeFilePath}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[QuestAudioPopulator] File download failed from URL '{audioUrl}': {www.error}");
                    }
                }
            }

            if (isSOModified)
            {
                EditorUtility.SetDirty(targetSO);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[QuestAudioPopulator] Audio population complete!");
        isProcessing = false;
    }
}
#endif