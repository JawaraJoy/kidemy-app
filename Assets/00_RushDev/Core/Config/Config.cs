using UnityEngine;

namespace Rush
{
    public abstract class Config : ScriptableObject
    {
        [System.Serializable]
        public class BasicInfo
        {
            [SerializeField]
            private string m_Id;
            [SerializeField]
            private string m_Name;
            [SerializeField, TextArea(3, 10)]
            private string m_Description;
            public string Id => m_Id;
            public string Name => m_Name;
            public string Description => m_Description;
        }

        [SerializeField]
        private BasicInfo m_Info;
        public BasicInfo Info => m_Info;
    }
}
