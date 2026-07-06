using System;
using System.Linq;
using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestWordBuilder", menuName = "EduGame/Quest/WordBuilder")]
    public class SO_QuestWordBuilder : SO_Quest
    {
        [Header("Question")]
        [SerializeField] private QuestUtilLabel question;
        [Space()]
        [SerializeField] private string answer = "";
        [Space()]
        [SerializeField] private QuestUtilLabelValue[] values;
        [Space()]
        
        [Header("Rule")]
        [SerializeField] private float timer = 0;
        [SerializeField] private bool randomizeChoice;

        public QuestUtilLabel Question => question;
        public string Answer => answer;
        public QuestUtilLabelValue[] Values => randomizeChoice ? GetRandomizedChoices() : values;
        public float Timer => timer;
        
        [NonSerialized] private QuestUtilLabelValue[] randomizedChoices;

        QuestUtilLabelValue[] GetRandomizedChoices()
        {
            if(randomizedChoices == null)
            {
                randomizedChoices = new QuestUtilLabelValue[values.Length]; 

                System.Random random = new System.Random();
                randomizedChoices = values.OrderBy(x => random.Next()).ToArray();
            }

            return randomizedChoices;
        }
    }
}