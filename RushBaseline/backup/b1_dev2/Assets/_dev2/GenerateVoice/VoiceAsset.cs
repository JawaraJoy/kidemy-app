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
        // OUTPUT
        // ============================================================

#if UNITY_EDITOR

        private const string OUTPUT_FOLDER =
            "Assets/_dev2/GenerateVoice/Voice";

#endif

        // ============================================================
        // VOICE DATA
        // ============================================================

        [Header("Voice")]

        [SerializeField]
        private string m_NameVoice;

        [TextArea(3, 10)]
        [SerializeField]
        private string m_Text;

        [SerializeField]
        private string m_VoiceId;

        [SerializeField]
        private AudioClip m_AudioClip;

#if UNITY_EDITOR

        // ============================================================
        // API
        // ============================================================

        [Header("API")]

        [SerializeField]
        private string m_ApiToken;

        [SerializeField]
        private bool m_IsGenerating;

#endif

        // ============================================================
        // PUBLIC ACCESS
        // ============================================================

        public string NameVoice => m_NameVoice;

        public string Text => m_Text;

        public string VoiceId => m_VoiceId;

        public AudioClip AudioClip => m_AudioClip;

        // ============================================================
        // GENERATE AUDIO
        // ============================================================

#if UNITY_EDITOR

        public void GenerateAudio()
        {
            // --------------------------------------------------------
            // Already generating
            // --------------------------------------------------------

            if (m_IsGenerating)
            {
                Debug.LogWarning(
                    $"[VoiceAsset] '{name}' is already generating."
                );

                return;
            }

            // --------------------------------------------------------
            // Validate Name
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(m_NameVoice))
            {
                Debug.LogError(
                    $"[VoiceAsset] Name Voice is empty on '{name}'."
                );

                return;
            }

            // --------------------------------------------------------
            // Validate Text
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(m_Text))
            {
                Debug.LogError(
                    $"[VoiceAsset] Text is empty on '{name}'."
                );

                return;
            }

            // --------------------------------------------------------
            // Validate API Token
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(m_ApiToken))
            {
                Debug.LogError(
                    $"[VoiceAsset] API Token is empty on '{name}'."
                );

                return;
            }

            // --------------------------------------------------------
            // Start
            // --------------------------------------------------------

            m_IsGenerating = true;

            EditorUtility.SetDirty(this);

            RepaintInspector();

            // --------------------------------------------------------
            // Voice ID
            // --------------------------------------------------------

            string voiceId =
                string.IsNullOrWhiteSpace(m_VoiceId)
                    ? VoiceAPI.DefaultVoiceId
                    : m_VoiceId;

            Debug.Log(
                $"[VoiceAsset] Starting generation...\n" +
                $"Name      : {m_NameVoice}\n" +
                $"Voice ID  : {voiceId}\n" +
                $"Text      : {m_Text}"
            );

            // --------------------------------------------------------
            // Request API
            // --------------------------------------------------------

            VoiceAPI.Generate(
                m_Text,
                voiceId,
                m_ApiToken,

                // ====================================================
                // SUCCESS
                // ====================================================

                audioUrl =>
                {
                    EditorCoroutineUtility.StartCoroutineOwnerless(
                        DownloadAudioRoutine(audioUrl)
                    );
                },

                // ====================================================
                // FAILURE
                // ====================================================

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
        // DOWNLOAD AUDIO
        // ============================================================

        private IEnumerator DownloadAudioRoutine(
            string audioUrl
        )
        {
            Debug.Log(
                $"[VoiceAsset] Downloading audio...\n" +
                $"{audioUrl}"
            );

            using UnityWebRequest request =
                UnityWebRequest.Get(audioUrl);

            yield return request.SendWebRequest();

            // --------------------------------------------------------
            // Download error
            // --------------------------------------------------------

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

            // --------------------------------------------------------
            // Get bytes
            // --------------------------------------------------------

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

            string voiceName =
                SanitizeFileName(
                    m_NameVoice
                );

            string fileName =
                $"Voice_{voiceName}";

            string fullFilePath =
                Path.Combine(
                    outputDirectory,
                    $"{fileName}.mp3"
                );

            // ========================================================
            // SAVE MP3
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
            // IMPORT INTO UNITY
            // ========================================================

            AssetDatabase.Refresh();

            // Give Unity a frame to import the MP3.
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

            // --------------------------------------------------------
            // Failed to import
            // --------------------------------------------------------

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
            // ASSIGN AUDIO CLIP
            // ========================================================

            Undo.RecordObject(
                this,
                "Generate Voice Audio"
            );

            m_AudioClip = clip;

            EditorUtility.SetDirty(this);

            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[VoiceAsset] ==============================\n" +
                $"[VoiceAsset] GENERATION SUCCESS\n" +
                $"[VoiceAsset] ==============================\n" +
                $"Name      : {m_NameVoice}\n" +
                $"Audio     : {clip.name}\n" +
                $"Path      : {unityAssetPath}\n" +
                $"=============================================="
            );

            // ========================================================
            // FINISH
            // ========================================================

            FinishGeneration();
        }

        // ============================================================
        // FINISH GENERATION
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

            return value.Trim();
        }

        // ============================================================
        // REPAINT INSPECTOR
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
        private class VoiceAssetEditor : UnityEditor.Editor
        {
            private SerializedProperty m_NameVoice;
            private SerializedProperty m_Text;
            private SerializedProperty m_VoiceId;
            private SerializedProperty m_AudioClip;

            private SerializedProperty m_ApiToken;
            private SerializedProperty m_IsGenerating;

            private void OnEnable()
            {
                m_NameVoice =
                    serializedObject.FindProperty(
                        "m_NameVoice"
                    );

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

                EditorGUILayout.Space(3);

                // ----------------------------------------------------
                // NAME
                // ----------------------------------------------------

                EditorGUILayout.PropertyField(
                    m_NameVoice,
                    new GUIContent("Name Voice")
                );

                // ----------------------------------------------------
                // TEXT
                // ----------------------------------------------------

                EditorGUILayout.PropertyField(
                    m_Text,
                    new GUIContent("Text")
                );

                // ----------------------------------------------------
                // VOICE ID
                // ----------------------------------------------------

                EditorGUILayout.PropertyField(
                    m_VoiceId,
                    new GUIContent("Voice ID")
                );

                // ----------------------------------------------------
                // AUDIO CLIP
                // ----------------------------------------------------

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

                EditorGUILayout.Space(3);

                EditorGUILayout.PropertyField(
                    m_ApiToken,
                    new GUIContent("API Token")
                );

                EditorGUILayout.Space(10);

                // ====================================================
                // GENERATE BUTTON
                // ====================================================

                bool isGenerating =
                    m_IsGenerating.boolValue;

                bool hasName =
                    !string.IsNullOrWhiteSpace(
                        asset.NameVoice
                    );

                bool hasText =
                    !string.IsNullOrWhiteSpace(
                        asset.Text
                    );

                bool hasToken =
                    !string.IsNullOrWhiteSpace(
                        m_ApiToken.stringValue
                    );

                bool canGenerate =
                    !isGenerating &&
                    hasName &&
                    hasText &&
                    hasToken;

                GUI.enabled =
                    canGenerate;

                if (
                    GUILayout.Button(
                        isGenerating
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
                // VALIDATION
                // ====================================================

                if (!hasName)
                {
                    EditorGUILayout.HelpBox(
                        "Name Voice is empty.",
                        MessageType.Warning
                    );
                }

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

                if (isGenerating)
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
                    $"{OUTPUT_FOLDER}/Voice_{asset.NameVoice}.mp3",
                    EditorStyles.textField,
                    GUILayout.Height(18)
                );

                serializedObject.ApplyModifiedProperties();
            }
        }

#endif
    }
}