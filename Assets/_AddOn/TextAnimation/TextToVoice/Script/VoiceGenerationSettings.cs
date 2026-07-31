using UnityEngine;

namespace AddOn.TextToSpeech
{
    [CreateAssetMenu(fileName = "VoiceGenerationSettings", menuName = "AddOn/Text To Speech/Settings")]
    public class VoiceGenerationSettings : ScriptableObject
    {
        [Header("OpenAI")]
        [SerializeField]
        private string m_ApiKey = string.Empty;

        [SerializeField]
        private string m_Model = "gpt-4o-mini-tts";

        [SerializeField]
        private string m_Voice = "alloy";

        [Header("Output")]
        [SerializeField]
        private string m_OutputFolder = "Assets/Generated Voices";

        public string ApiKey => m_ApiKey;
        public string Model => m_Model;
        public string Voice => m_Voice;
        public string OutputFolder => m_OutputFolder;
    }
}