using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestMultipleChoiceNarrative", menuName = "EduGame/Quest/MultipleChoiceNarrative")]
    public class SO_QuestMultipleChoiceNarrative : SO_QuestMultipleChoice
    {
        [Header("Question")]
        [SerializeField] [FormerlySerializedAs("narrative")] private string m_Narrative;

        public string Narrative => m_Narrative;
    }
}