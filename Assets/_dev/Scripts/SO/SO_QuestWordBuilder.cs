using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestWordBuilder", menuName = "EduGame/Quest/WordBuilder")]
    public class SO_QuestWordBuilder : SO_Quest
    {
        [Header("Question")]
        [SerializeField] [FormerlySerializedAs("answer")] private string m_Answer = "";
        [Space()]
        [SerializeField] [FormerlySerializedAs("values")] private QuestUtilLabelValue[] m_Values;
        [Space()]
        
        [Header("Rule")]
        [SerializeField] [FormerlySerializedAs("timer")] private float m_Timer = 0;
        [SerializeField] [FormerlySerializedAs("randomizeChoice")] private bool m_RandomizeChoice;

        public string Answer => m_Answer;
        public QuestUtilLabelValue[] Values => m_RandomizeChoice ? GetRandomizedChoices() : m_Values;
        public float Timer => m_Timer;
        
        [NonSerialized] private QuestUtilLabelValue[] m_RandomizedChoices;

        QuestUtilLabelValue[] GetRandomizedChoices()
        {
            if(m_RandomizedChoices == null)
            {
                m_RandomizedChoices = new QuestUtilLabelValue[m_Values.Length]; 

                System.Random random = new System.Random();
                m_RandomizedChoices = m_Values.OrderBy(x => random.Next()).ToArray();
            }

            return m_RandomizedChoices;
        }
    }
}