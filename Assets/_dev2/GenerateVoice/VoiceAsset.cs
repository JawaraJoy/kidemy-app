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

        private const string c_OutputFolder =
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

            new VoiceAPI().Generate(
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
                    c_OutputFolder
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
                $"{c_OutputFolder}/{fileName}.mp3";

            unityAssetPath =
                unityAssetPath.Replace(
                    c_WindowsSeparator,
                    c_UnitySeparator
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
                c_UndoGenerate
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
                        c_InvalidCharReplacement
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

        private const string c_WindowsSeparator = "\\";
        private const string c_UnitySeparator = "/";
        private const string c_UndoGenerate = "Generate Voice Audio";
        private const string c_InvalidCharReplacement = "_";

        // ============================================================
        // CUSTOM INSPECTOR
        // ============================================================

        [CustomEditor(typeof(VoiceAsset))]
        private class VoiceAssetEditor : UnityEditor.Editor
        {
            private const string c_NameVoiceProperty = "m_NameVoice";
            private const string c_TextProperty = "m_Text";
            private const string c_VoiceIdProperty = "m_VoiceId";
            private const string c_AudioClipProperty = "m_AudioClip";
            private const string c_ApiTokenProperty = "m_ApiToken";
            private const string c_IsGeneratingProperty = "m_IsGenerating";
            private const string c_VoiceHeader = "Voice";
            private const string c_NameVoiceLabel = "Name Voice";
            private const string c_TextLabel = "Text";
            private const string c_VoiceIdLabel = "Voice ID";
            private const string c_AudioClipLabel = "Audio Clip";
            private const string c_ApiHeader = "API";
            private const string c_ApiTokenLabel = "API Token";
            private const string c_GeneratingButton = "Generating...";
            private const string c_GenerateButton = "Generate Audio";
            private const string c_HelpNameEmpty = "Name Voice is empty.";
            private const string c_HelpTextEmpty = "Text is empty.";
            private const string c_HelpTokenEmpty = "API Token is empty.";
            private const string c_HelpGenerating = "Generating audio...";
            private const string c_OutputHeader = "Output";
            private const int c_SpaceSmall = 3;
            private const int c_SpaceLarge = 10;
            private const int c_ButtonHeight = 40;
            private const int c_PathHeight = 18;

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
                        c_NameVoiceProperty
                    );

                m_Text =
                    serializedObject.FindProperty(
                        c_TextProperty
                    );

                m_VoiceId =
                    serializedObject.FindProperty(
                        c_VoiceIdProperty
                    );

                m_AudioClip =
                    serializedObject.FindProperty(
                        c_AudioClipProperty
                    );

                m_ApiToken =
                    serializedObject.FindProperty(
                        c_ApiTokenProperty
                    );

                m_IsGenerating =
                    serializedObject.FindProperty(
                        c_IsGeneratingProperty
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
                    c_VoiceHeader,
                    EditorStyles.boldLabel
                );

                EditorGUILayout.Space(c_SpaceSmall);

                // ----------------------------------------------------
                // NAME
                // ----------------------------------------------------

                EditorGUILayout.PropertyField(
                    m_NameVoice,
                    new GUIContent(c_NameVoiceLabel)
                );

                // ----------------------------------------------------
                // TEXT
                // ----------------------------------------------------

                EditorGUILayout.PropertyField(
                    m_Text,
                    new GUIContent(c_TextLabel)
                );

                // ----------------------------------------------------
                // VOICE ID
                // ----------------------------------------------------

                EditorGUILayout.PropertyField(
                    m_VoiceId,
                    new GUIContent(c_VoiceIdLabel)
                );

                // ----------------------------------------------------
                // AUDIO CLIP
                // ----------------------------------------------------

                EditorGUILayout.PropertyField(
                    m_AudioClip,
                    new GUIContent(c_AudioClipLabel)
                );

                EditorGUILayout.Space(c_SpaceLarge);

                // ====================================================
                // API
                // ====================================================

                EditorGUILayout.LabelField(
                    c_ApiHeader,
                    EditorStyles.boldLabel
                );

                EditorGUILayout.Space(c_SpaceSmall);

                EditorGUILayout.PropertyField(
                    m_ApiToken,
                    new GUIContent(c_ApiTokenLabel)
                );

                EditorGUILayout.Space(c_SpaceLarge);

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
                            ? c_GeneratingButton
                            : c_GenerateButton,
                        GUILayout.Height(c_ButtonHeight)
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
                        c_HelpNameEmpty,
                        MessageType.Warning
                    );
                }

                if (!hasText)
                {
                    EditorGUILayout.HelpBox(
                        c_HelpTextEmpty,
                        MessageType.Warning
                    );
                }

                if (!hasToken)
                {
                    EditorGUILayout.HelpBox(
                        c_HelpTokenEmpty,
                        MessageType.Warning
                    );
                }

                if (isGenerating)
                {
                    EditorGUILayout.HelpBox(
                        c_HelpGenerating,
                        MessageType.Info
                    );
                }

                // ====================================================
                // OUTPUT
                // ====================================================

                EditorGUILayout.Space(c_SpaceLarge);

                EditorGUILayout.LabelField(
                    c_OutputHeader,
                    EditorStyles.boldLabel
                );

                EditorGUILayout.SelectableLabel(
                    $"{c_OutputFolder}/Voice_{asset.NameVoice}.mp3",
                    EditorStyles.textField,
                    GUILayout.Height(c_PathHeight)
                );

                serializedObject.ApplyModifiedProperties();
            }
        }

#endif
    }
}