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
    }
}
