#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.EditorCoroutines.Editor; // Package for Edit-Mode Coroutines
using EduGame; // Matches your project namespace

public class QuestAudioPopulatorWindow : EditorWindow
{
    [SerializeField] [FormerlySerializedAs("targetQuests")] private SO_Quest[] m_TargetQuests = new SO_Quest[0];
    private bool m_ForceRedownload = false;
    private bool m_IsProcessing = false;

    [MenuItem("Tools/Audio/Populate Quest Audio")]
    public static void ShowWindow()
    {
        GetWindow<QuestAudioPopulatorWindow>("Quest Audio Populator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Quest Audio Batch Populator", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        SerializedObject serializedWindow = new SerializedObject(this);
        serializedWindow.Update();
        EditorGUILayout.PropertyField(
            serializedWindow.FindProperty(nameof(m_TargetQuests)),
            new GUIContent("Quest Assets"),
            true);
        serializedWindow.ApplyModifiedProperties();

        EditorGUILayout.HelpBox(
            "Add the SO_Quest assets that should have their audio populated.",
            MessageType.Info);

        m_ForceRedownload = EditorGUILayout.Toggle("Force Redownload Existing", m_ForceRedownload);

        EditorGUILayout.Space(10);

        GUI.enabled = !m_IsProcessing;
        if (GUILayout.Button("Populate Selected Quest Audio", GUILayout.Height(35)))
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

        m_IsProcessing = true;

        if (m_TargetQuests == null || m_TargetQuests.Length == 0)
        {
            Debug.LogWarning("[QuestAudioPopulator] No SO_Quest assets were assigned.");
            m_IsProcessing = false;
            yield break;
        }

        Debug.Log($"[QuestAudioPopulator] Processing {m_TargetQuests.Length} assigned SO_Quest slots.");
        HashSet<SO_Quest> processedQuests = new HashSet<SO_Quest>();

        foreach (SO_Quest targetSO in m_TargetQuests)
        {
            if (targetSO == null || !processedQuests.Add(targetSO))
                continue;

            // Get VoiceRequests directly from the assigned ScriptableObject.
            VoiceRequest[] requests = targetSO.GetVoiceRequests();
            if (requests == null || requests.Length == 0) continue;

            bool isSOModified = false;

            foreach (var req in requests)
            {
                // Skip existing audio clips unless Force Redownload is checked
                if (!m_ForceRedownload && targetSO.HasVoiceClip(req.id))
                {
                    Debug.Log($"[QuestAudioPopulator] Skipping '{targetSO.name}' -> ID '{req.id}' (Audio exists)");
                    continue;
                }

                if (string.IsNullOrEmpty(req.text)) continue;

                Debug.Log($"[QuestAudioPopulator] Requesting Audio for '{targetSO.name}' | VoiceID: '{req.voice_id}' | ID: '{req.id}'");

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
                            targetSO.AssignVoiceClip(req.id, importedClip);
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
        m_IsProcessing = false;
    }

}
#endif
