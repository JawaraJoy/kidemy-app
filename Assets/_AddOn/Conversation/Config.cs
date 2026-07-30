using UnityEngine;

namespace TextToVoice
{
    public abstract class Config : ScriptableObject
    {
        [System.Serializable]
        public class Identification
        {
            [SerializeField]
            private string m_Id = string.Empty;
            [SerializeField]
            private string m_Name = string.Empty;
            [SerializeField, TextArea]
            private string m_Description = string.Empty;
            public string Id => m_Id;
            public string Name => m_Name;
            public string Description => m_Description;
        }
        [SerializeField]
        private Identification m_Identic;
        public Identification Identic => m_Identic;
    }
}
