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
        private SO_Quest[] m_QuestConfigs;
        [Header("Per-question overrides (optional; empty or null = use default above)")]
        [SerializeField]
        private Quest[] m_QuestLayoutOverrides;
        [SerializeField]
        private Sprite[] m_BackgroundOverrides;
        [SerializeField]
        private QuestComponent[] m_ChoiceOverrides;
        [Header("Identity (empty = use asset name)")]
        [SerializeField]
        private string m_Id;

        public SO_Character Character => m_Character;
        public QuestComponent ChoicePrefab => m_ChoicePrefab;
        public SceneConfig SceneConfig => m_SceneConfig;
        public Sprite Background => m_Background;
        public Quest QuestLayoutPrefabs => m_QuestLayoutPrefabs;

        public Quest GetLayout(int index)
        {
            if (m_QuestLayoutOverrides != null && index >= 0 && index < m_QuestLayoutOverrides.Length && m_QuestLayoutOverrides[index])
                return m_QuestLayoutOverrides[index];
            return m_QuestLayoutPrefabs;
        }

        public string Id => string.IsNullOrEmpty(m_Id) ? name : m_Id;

        public QuestComponent GetChoicePrefab(int index)
        {
            if (m_ChoiceOverrides != null && index >= 0 && index < m_ChoiceOverrides.Length && m_ChoiceOverrides[index])
                return m_ChoiceOverrides[index];
            return m_ChoicePrefab;
        }

        public Sprite GetBackground(int index)
        {
            if (m_BackgroundOverrides != null && index >= 0 && index < m_BackgroundOverrides.Length && m_BackgroundOverrides[index])
                return m_BackgroundOverrides[index];
            return m_Background;
        }
        public SO_Quest[] QuestConfigs => m_QuestConfigs;
        public ParticleSystem ConffetyVFXPrefab => m_ConffetyVFXPrefab;
        public ReactionConfig RightReaction => m_Character ? m_Character.RightReaction : null;
        public ReactionConfig WrongReaction => m_Character ? m_Character.WrongReaction : null;
        public ResultPanel ResultPanelPrefab => m_ResultPanelPrefab;
        public ReactionPanel ReactionPanelPrefab => m_ReactionPanelPrefab;

    }
}
