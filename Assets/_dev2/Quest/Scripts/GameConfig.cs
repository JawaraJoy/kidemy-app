using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "OtherDev/GameConfig", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [SerializeField, TextArea]
        private string m_Note;
        [SerializeField]
        private ChallengeConfig[] m_ChallengeConfigs;

        public ChallengeConfig[] ChallengeConfigs => m_ChallengeConfigs;

        public ChallengeConfig Find(string id)
        {
            if (m_ChallengeConfigs == null || string.IsNullOrEmpty(id))
                return null;
            for (int i = 0; i < m_ChallengeConfigs.Length; i++)
            {
                if (m_ChallengeConfigs[i] && m_ChallengeConfigs[i].Id == id)
                    return m_ChallengeConfigs[i];
            }
            return null;
        }
    }
}
