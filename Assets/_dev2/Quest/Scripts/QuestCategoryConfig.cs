using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "QuestCategory_", menuName = "OtherDev/QuestCategoryConfig", order = 0)]
    public class QuestCategoryConfig : ScriptableObject
    {
        [SerializeField]
        private string m_CategoryName;
        public string CategoryName => m_CategoryName;   
    }
}
