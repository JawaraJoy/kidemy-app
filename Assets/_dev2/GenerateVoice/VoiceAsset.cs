using System;
using System.Collections;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Networking;
#endif

namespace EduGame
{
    [CreateAssetMenu(
        fileName = "VoiceAsset",
        menuName = "EduGame/Generate Voice/Voice Asset"
    )]
    public class VoiceAsset : ScriptableObject
    {
        // ============================================================
        // CONFIGURATION
        // ============================================================

#if UNITY_EDITOR

        private const string OUTPUT_FOLDER =
            "Assets/_dev2/GenerateVoice/Voice";

#endif

        // ============================================================
        // DATA
        // ============================================================

        [Header("Voice")]

        [TextArea(3, 10)]
        [SerializeField]
        private string m_Text;

        [SerializeField]
        private string m_VoiceId;

        [SerializeField]
        private AudioClip m_AudioClip;

#if UNITY_EDITOR

        [Header("API")]

        [SerializeField]
        private string m_ApiToken;

        [SerializeField]
        private bool m_IsGenerating;

#endif

        // ============================================================
        // PUBLIC
        // ============================================================

        public string Text =>
            m_Text;

        public string VoiceId =>
            m_VoiceId;

        public AudioClip AudioClip =>
            m_AudioClip;

        // ============================================================
        // GENERATE
        // ============================================================

#if UNITY_EDITOR

        public void GenerateAudio()
        {
            if (m_IsGenerating)
            {
                Debug.LogWarning(
                    $"[VoiceAsset] '{name}' is already generating."
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(m_Text))
            {
                Debug.LogError(
                    $"[VoiceAsset] Text is empty on '{name}'."
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(m_ApiToken))
            {
                Debug.LogError(
                    $"[VoiceAsset] API Token is empty on '{name}'."
                );

                return;
            }

            m_IsGenerating = true;

            EditorUtility.SetDirty(this);

            RepaintInspector();

            string voiceId =
                string.IsNullOrWhiteSpace(m_VoiceId)
                    ? VoiceAPI.DefaultVoiceId
                    : m_VoiceId;

            Debug.Log(
                $"[VoiceAsset] Generating '{name}'...\n" +
                $"Voice ID: {voiceId}"
            );

            VoiceAPI.Generate(
                m_Text,
                voiceId,
                m_ApiToken,

                // SUCCESS
                audioUrl =>
                {
                    EditorCoroutineUtility.StartCoroutineOwnerless(
                        DownloadAudioRoutine(audioUrl)
                    );
                },

                // FAILURE
                error =>
                {
                    Debug.LogError(
                        $"[VoiceAsset] Generation failed:\n{error}"
                    );

                    FinishGeneration();
                }
            );
        }

        // ============================================================
        // DOWNLOAD
        // ============================================================

        private IEnumerator DownloadAudioRoutine(
            string audioUrl
        )
        {
            Debug.Log(
                $"[VoiceAsset] Downloading audio..."
            );

            using UnityWebRequest request =
                UnityWebRequest.Get(audioUrl);

            yield return request.SendWebRequest();

            if (
                request.result !=
                UnityWebRequest.Result.Success
            )
            {
                Debug.LogError(
                    $"[VoiceAsset] Audio download failed:\n" +
                    $"{request.error}"
                );

                FinishGeneration();

                yield break;
            }

            byte[] audioData =
                request.downloadHandler.data;

            if (
                audioData == null ||
                audioData.Length == 0
            )
            {
                Debug.LogError(
                    "[VoiceAsset] Downloaded audio is empty."
                );

                FinishGeneration();

                yield break;
            }

            // ========================================================
            // OUTPUT DIRECTORY
            // ========================================================

            string projectPath =
                Directory.GetParent(
                    Application.dataPath
                ).FullName;

            string outputDirectory =
                Path.Combine(
                    projectPath,
                    OUTPUT_FOLDER
                );

            outputDirectory =
                Path.GetFullPath(
                    outputDirectory
                );

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(
                    outputDirectory
                );
            }

            // ========================================================
            // FILE NAME
            // ========================================================

            string fileName =
                SanitizeFileName(name);

            string fullFilePath =
                Path.Combine(
                    outputDirectory,
                    fileName + ".mp3"
                );

            // ========================================================
            // SAVE FILE
            // ========================================================

            try
            {
                File.WriteAllBytes(
                    fullFilePath,
                    audioData
                );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[VoiceAsset] Failed to save audio:\n" +
                    $"{exception}"
                );

                FinishGeneration();

                yield break;
            }

            Debug.Log(
                $"[VoiceAsset] Audio saved:\n" +
                $"{fullFilePath}"
            );

            // ========================================================
            // IMPORT ASSET
            // ========================================================

            AssetDatabase.Refresh();

            // Give Unity time to import.
            yield return null;

            string unityAssetPath =
                $"{OUTPUT_FOLDER}/{fileName}.mp3";

            unityAssetPath =
                unityAssetPath.Replace(
                    "\\",
                    "/"
                );

            Debug.Log(
                $"[VoiceAsset] Loading AudioClip:\n" +
                $"{unityAssetPath}"
            );

            AudioClip clip =
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    unityAssetPath
                );

            if (clip == null)
            {
                Debug.LogError(
                    $"[VoiceAsset] Unity could not load " +
                    $"AudioClip:\n{unityAssetPath}"
                );

                FinishGeneration();

                yield break;
            }

            // ========================================================
            // ASSIGN
            // ========================================================

            Undo.RecordObject(
                this,
                "Generate Voice Audio"
            );

            m_AudioClip = clip;

            EditorUtility.SetDirty(this);

            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[VoiceAsset] SUCCESS!\n" +
                $"Voice Asset : {name}\n" +
                $"Audio Clip  : {clip.name}\n" +
                $"Path        : {unityAssetPath}"
            );

            FinishGeneration();
        }

        // ============================================================
        // FINISH
        // ============================================================

        private void FinishGeneration()
        {
            m_IsGenerating = false;

            EditorUtility.SetDirty(this);

            AssetDatabase.SaveAssets();

            RepaintInspector();
        }

        // ============================================================
        // SANITIZE FILE NAME
        // ============================================================

        private static string SanitizeFileName(
            string value
        )
        {
            foreach (
                char character
                in Path.GetInvalidFileNameChars()
            )
            {
                value =
                    value.Replace(
                        character.ToString(),
                        "_"
                    );
            }

            return value;
        }

        // ============================================================
        // REPAINT
        // ============================================================

        private void RepaintInspector()
        {
            UnityEditorInternal.InternalEditorUtility
                .RepaintAllViews();
        }

        // ============================================================
        // CUSTOM INSPECTOR
        // ============================================================

        [CustomEditor(typeof(VoiceAsset))]
        private class VoiceAssetEditor : Editor
        {
            private SerializedProperty m_Text;
            private SerializedProperty m_VoiceId;
            private SerializedProperty m_AudioClip;
            private SerializedProperty m_ApiToken;
            private SerializedProperty m_IsGenerating;

            private void OnEnable()
            {
                m_Text =
                    serializedObject.FindProperty(
                        "m_Text"
                    );

                m_VoiceId =
                    serializedObject.FindProperty(
                        "m_VoiceId"
                    );

                m_AudioClip =
                    serializedObject.FindProperty(
                        "m_AudioClip"
                    );

                m_ApiToken =
                    serializedObject.FindProperty(
                        "m_ApiToken"
                    );

                m_IsGenerating =
                    serializedObject.FindProperty(
                        "m_IsGenerating"
                    );
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                VoiceAsset asset =
                    (VoiceAsset)target;

                // ====================================================
                // VOICE
                // ====================================================

                EditorGUILayout.LabelField(
                    "Voice",
                    EditorStyles.boldLabel
                );

                EditorGUILayout.PropertyField(
                    m_Text,
                    new GUIContent("Text")
                );

                EditorGUILayout.PropertyField(
                    m_VoiceId,
                    new GUIContent("Voice ID")
                );

                EditorGUILayout.PropertyField(
                    m_AudioClip,
                    new GUIContent("Audio Clip")
                );

                EditorGUILayout.Space(10);

                // ====================================================
                // API
                // ====================================================

                EditorGUILayout.LabelField(
                    "API",
                    EditorStyles.boldLabel
                );

                EditorGUILayout.PropertyField(
                    m_ApiToken,
                    new GUIContent("API Token")
                );

                EditorGUILayout.Space(10);

                // ====================================================
                // GENERATE
                // ====================================================

                bool generating =
                    m_IsGenerating.boolValue;

                bool hasText =
                    !string.IsNullOrWhiteSpace(
                        asset.Text
                    );

                bool hasToken =
                    !string.IsNullOrWhiteSpace(
                        m_ApiToken.stringValue
                    );

                GUI.enabled =
                    !generating &&
                    hasText &&
                    hasToken;

                if (
                    GUILayout.Button(
                        generating
                            ? "Generating..."
                            : "Generate Audio",
                        GUILayout.Height(40)
                    )
                )
                {
                    asset.GenerateAudio();
                }

                GUI.enabled = true;

                // ====================================================
                // WARNINGS
                // ====================================================

                if (!hasText)
                {
                    EditorGUILayout.HelpBox(
                        "Text is empty.",
                        MessageType.Warning
                    );
                }

                if (!hasToken)
                {
                    EditorGUILayout.HelpBox(
                        "API Token is empty.",
                        MessageType.Warning
                    );
                }

                if (generating)
                {
                    EditorGUILayout.HelpBox(
                        "Generating audio...",
                        MessageType.Info
                    );
                }

                // ====================================================
                // OUTPUT
                // ====================================================

                EditorGUILayout.Space(10);

                EditorGUILayout.LabelField(
                    "Output",
                    EditorStyles.boldLabel
                );

                EditorGUILayout.SelectableLabel(
                    OUTPUT_FOLDER,
                    EditorStyles.textField,
                    GUILayout.Height(18)
                );

                serializedObject.ApplyModifiedProperties();
            }
        }

#endif
    }
}