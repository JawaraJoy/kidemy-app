using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "Challenge_", menuName = "OtherDev/ChallengeConfig", order = 0)]
    public class ChallengeConfig : ScriptableObject
    {
        [SerializeField]
        private Sprite m_Background;
        
        [Header("Prefabs")]
        [SerializeField]
        private ParticleSystem m_ConffetyVFXPrefab;
        [SerializeField]
        private QuestComponent m_ChoicePrefab;
        [SerializeField]
        private Quest m_QuestLayoutPrefabs;
        [SerializeField]
        private ReactionPanel m_ReactionPanelPrefab;
        [SerializeField]
        private ResultPanel m_ResultPanelPrefab;
        [Header("Configs")]
        [SerializeField]
        private SO_Character m_Character;
        [SerializeField]
        private SceneConfig m_SceneConfig;
        [SerializeField]
        private ReactionConfig m_RightReaction;
        [SerializeField]
        private ReactionConfig m_WrongReaction;
        [SerializeField]
        private SO_Quest[] m_QuestConfigs;

        public SO_Character Character => m_Character;
        public QuestComponent ChoicePrefab => m_ChoicePrefab;
        public SceneConfig SceneConfig => m_SceneConfig;
        public Sprite Background => m_Background;
        public Quest QuestLayoutPrefabs => m_QuestLayoutPrefabs;
        public SO_Quest[] QuestConfigs => m_QuestConfigs;
        public ParticleSystem ConffetyVFXPrefab => m_ConffetyVFXPrefab;
        public ReactionConfig RightReaction => m_RightReaction;
        public ReactionConfig WrongReaction => m_WrongReaction;
        public ResultPanel ResultPanelPrefab => m_ResultPanelPrefab;
        public ReactionPanel ReactionPanelPrefab => m_ReactionPanelPrefab;

    }
}
