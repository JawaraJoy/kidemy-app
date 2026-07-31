using AddOn.TextAnimation;
using UnityEditor;
using UnityEngine;

namespace AddOn.TextToSpeech.Editor
{
    public class VoiceGeneratorWindow : EditorWindow
    {
        private VoiceGenerationService m_Service;

        private string m_OutputFolder = "Assets/Generated Voices";

        [MenuItem("Tools/Text To Speech/Voice Generator")]
        public static void Open()
        {
            GetWindow<VoiceGeneratorWindow>("Voice Generator");
        }

        private void OnEnable()
        {
            m_Service = new VoiceGenerationService();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            EditorGUILayout.LabelField(
                "OpenAI Text To Speech",
                EditorStyles.boldLabel);

            GUILayout.Space(10);

            m_OutputFolder = EditorGUILayout.TextField(
                "Output Folder",
                m_OutputFolder);

            GUILayout.Space(10);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Selected Dialogue",
                    GetSelectedDialogueName());
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Generate Selected", GUILayout.Height(30)))
            {
                GenerateSelected();
            }

            if (GUILayout.Button("Generate All", GUILayout.Height(30)))
            {
                GenerateAll();
            }
        }

        private string GetSelectedDialogueName()
        {
            if (Selection.activeObject is DialogueConfig dialogue)
                return dialogue.name;

            return "None";
        }

        private void GenerateSelected()
        {
            if (Selection.activeObject is not DialogueConfig dialogue)
            {
                Debug.LogWarning("Please select a DialogueConfig.");
                return;
            }

            Debug.Log($"Generate : {dialogue.name}");

            // nanti
            // m_Service.Generate(dialogue);
        }

        private void GenerateAll()
        {
            string[] guids =
                AssetDatabase.FindAssets("t:DialogueConfig");

            foreach (string guid in guids)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(guid);

                DialogueConfig dialogue =
                    AssetDatabase.LoadAssetAtPath<DialogueConfig>(path);

                Debug.Log($"Generate : {dialogue.name}");

                // nanti
                // m_Service.Generate(dialogue);
            }
        }
    }
}