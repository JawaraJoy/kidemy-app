using UnityEngine;

namespace AddOn.textto
{
    public class VoiceGeneratorSetting : ScriptableObject
    {
        [SerializeField]
        private string m_ApiKey;

        [SerializeField]
        private string m_Model;

        [SerializeField]
        private string m_Voice;

        [SerializeField]
        private string m_OutputFolder;
    }
}
