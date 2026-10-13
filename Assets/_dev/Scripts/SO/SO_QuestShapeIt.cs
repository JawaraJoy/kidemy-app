using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestShapeIt", menuName = "EduGame/Quest/ShapeIt")]
    public class SO_QuestShapeIt : SO_Quest
    {
         [Header("Question")]
        [SerializeField] [FormerlySerializedAs("canvas")] private QuestShapeItCanvas m_Canvas;
        
        [Header("Rule")]
        [SerializeField] [FormerlySerializedAs("timer")] private float m_Timer = 0;
        [SerializeField] [FormerlySerializedAs("randomizeChoice")] private bool m_RandomizeChoice;

        public QuestShapeItCanvas Canvas => m_Canvas;
        public QuestDragNDropItem[] Items => m_RandomizeChoice ? GetRandomizedChoices() : m_Canvas.Items;
        public float Timer => m_Timer;
        
        [NonSerialized] private QuestDragNDropItem[] m_RandomizedChoices;

        QuestDragNDropItem[] GetRandomizedChoices()
        {
            if(m_RandomizedChoices == null)
            {
                m_RandomizedChoices = new QuestDragNDropItem[m_Canvas.Items.Length]; 

                System.Random random = new System.Random();
                m_RandomizedChoices = m_Canvas.Items.OrderBy(x => random.Next()).ToArray();
            }

            return m_RandomizedChoices;
        }
    }
}