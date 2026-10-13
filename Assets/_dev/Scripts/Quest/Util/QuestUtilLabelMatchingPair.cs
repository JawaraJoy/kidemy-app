using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestUtilLabelMatchingPair
    {
        [SerializeField] [FormerlySerializedAs("matchA")] private QuestUtilLabelMatching m_MatchA;
        [SerializeField] [FormerlySerializedAs("matchB")] private QuestUtilLabelMatching m_MatchB;

        public QuestUtilLabelMatching MatchA => m_MatchA;
        public QuestUtilLabelMatching MatchB => m_MatchB;
    }
}
