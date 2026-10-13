using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestMultipleChoiceFinder", menuName = "EduGame/Quest/MultipleChoiceFinder")]
    public class SO_QuestMultipleChoiceFinder : SO_Quest
    {
        [Header("Question")]
        [SerializeField] [FormerlySerializedAs("choices")] private QuestUtilLabelChoice[] m_Choices;
        
    }
}