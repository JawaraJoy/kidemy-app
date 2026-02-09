using System;
using System.Linq;
using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestMultipleChoice", menuName = "EduGame/Quest/MultipleChoice")]
    public class SO_QuestMultipleChoice : SO_Quest
    {
        [Header("Question")]
        [SerializeField] private QuestUtilLabel question;
        [Space()]
        [SerializeField] private QuestUtilLabelChoice[] choices;
        [Space()]
        
        [Header("Rule")]
        [SerializeField] private float timer = 0;
        [SerializeField] private bool randomizeChoice;

        public QuestUtilLabel Question => question;
        public QuestUtilLabelChoice[] Choices => randomizeChoice ? GetRandomizedChoices() : choices;
        public float Timer => timer;
        public int TotalAnswer => GetTotalAnswer();

        [NonSerialized] private int totalAnswer = 0;
        [NonSerialized] private QuestUtilLabelChoice[] randomizedChoices;

        QuestUtilLabelChoice[] GetRandomizedChoices()
        {
            if(randomizedChoices == null)
            {
                randomizedChoices = new QuestUtilLabelChoice[choices.Length]; 

                System.Random random = new System.Random();
                randomizedChoices = choices.OrderBy(x => random.Next()).ToArray();
            }

            return randomizedChoices;
        }

        int GetTotalAnswer()
        {
            if(totalAnswer == 0)
            {
                foreach (var choice in choices)
                {
                    if(choice.IsAnswer)
                        totalAnswer++;
                }
            }

            return totalAnswer;
        }
    }
}