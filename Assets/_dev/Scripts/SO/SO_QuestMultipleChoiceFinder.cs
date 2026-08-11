using System;
using System.Linq;
using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestMultipleChoiceFinder", menuName = "EduGame/Quest/MultipleChoiceFinder")]
    public class SO_QuestMultipleChoiceFinder : SO_Quest
    {
        [Header("Question")]
        [SerializeField] private QuestUtilLabelChoice[] choices;
        
    }
}