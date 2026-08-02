using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestMultipleChoiceNarrative", menuName = "EduGame/Quest/MultipleChoiceNarrative")]
    public class SO_QuestMultipleChoiceNarrative : SO_QuestMultipleChoice
    {
        [Header("Question")]
        [SerializeField] private string narrative;

        public string Narrative => narrative;
    }
}