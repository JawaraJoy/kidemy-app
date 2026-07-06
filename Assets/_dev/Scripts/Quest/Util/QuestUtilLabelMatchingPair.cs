using UnityEngine;

namespace EduGame
{
    public class QuestUtilLabelMatchingPair
    {
        [SerializeField] private QuestUtilLabelMatching matchA;
        [SerializeField] private QuestUtilLabelMatching matchB;

        public QuestUtilLabelMatching MatchA => matchA;
        public QuestUtilLabelMatching MatchB => matchB;
    }
}
