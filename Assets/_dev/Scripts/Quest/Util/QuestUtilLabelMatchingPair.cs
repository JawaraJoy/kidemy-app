using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestUtilLabelMatchingPair
    {
        [SerializeField] [FormerlySerializedAs("matchA")] private QuestUtilLabel m_MatchA;
        [SerializeField] [FormerlySerializedAs("matchB")] private QuestUtilLabel m_MatchB;

        public QuestUtilLabel MatchA => m_MatchA;
        public QuestUtilLabel MatchB => m_MatchB;
    }
}
