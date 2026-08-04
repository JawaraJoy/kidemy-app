using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "ChallengeConfig", menuName = "OtherDev/ChallengeConfig", order = 0)]
    public class ChallengeConfig : ScriptableObject
    {
        [SerializeField]
        private Quest m_QuestLayoutPrefabs;
        [SerializeField]
        private SO_Quest[] m_QuestConfigs;

        public Quest QuestLayoutPrefabs => m_QuestLayoutPrefabs;
        public SO_Quest[] QuestConfigs => m_QuestConfigs;
    }
}
