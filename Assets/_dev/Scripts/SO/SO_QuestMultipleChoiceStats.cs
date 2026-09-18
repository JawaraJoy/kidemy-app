using System;
using System.Linq;
using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestMultipleChoiceStats", menuName = "EduGame/Quest/MultipleChoiceStats")]
    public class SO_QuestMultipleChoiceStats : SO_QuestMultipleChoice
    {
        [Header("Question")]
        
        [SerializeField] private QuestUtilLabel[] stats;

        public QuestUtilLabel[] Stats => stats;
    }
}