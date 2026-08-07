using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "Challenge_", menuName = "OtherDev/ChallengeConfig", order = 0)]
    public class ChallengeConfig : ScriptableObject
    {
        [SerializeField]
        private SceneConfig m_SceneConfig;
        [SerializeField]
        private QuestComponent m_ChoicePrefab;
        [SerializeField]
        private Quest m_QuestLayoutPrefabs;
        [SerializeField]
        private Sprite m_Background;
        [SerializeField]
        private SO_Quest[] m_QuestConfigs;

        public QuestComponent ChoicePrefab => m_ChoicePrefab;
        public SceneConfig SceneConfig => m_SceneConfig;
        public Sprite Background => m_Background;
        public Quest QuestLayoutPrefabs => m_QuestLayoutPrefabs;
        public SO_Quest[] QuestConfigs => m_QuestConfigs;

    }
}
