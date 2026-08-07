using UnityEngine;

namespace EduGame
{
    public class Config : ScriptableObject
    {
        [System.Serializable]
        public class Identifield
        {
            [SerializeField]
            private string m_Id;
            [SerializeField]
            private string m_Name;
            [SerializeField]
            private string m_Description;
            public string Id => m_Id;
            public string Name => m_Name;
            public string Description => m_Description;
        }
        [SerializeField]
        private Identifield m_Identic;
        public Identifield Identic => m_Identic;
    }
}
