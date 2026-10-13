using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestRythimMaker", menuName = "EduGame/Quest/RythimMaker")]
    public class SO_QuestRythimMaker : SO_Quest
    {
        [Header("Question")]
        [SerializeField] [FormerlySerializedAs("speed")] private float m_Speed = 1f;
        [SerializeField] [FormerlySerializedAs("notes")] private QuestRythimNoteItem[] m_Notes;
        
        [Header("Rule")]
        [SerializeField] [FormerlySerializedAs("timer")] private float m_Timer = 0;
        [SerializeField] [FormerlySerializedAs("randomizeChoice")] private bool m_RandomizeChoice;

        public QuestRythimNoteItem[] Items => m_RandomizeChoice ? GetRandomizedChoices() : m_Notes;
        public float Timer => m_Timer;
        public float Speed => m_Speed;
        
        [NonSerialized] private QuestRythimNoteItem[] m_RandomizedNotes;

        QuestRythimNoteItem[] GetRandomizedChoices()
        {
            if(m_RandomizedNotes == null)
            {
                m_RandomizedNotes = new QuestRythimNoteItem[m_Notes.Length]; 

                System.Random random = new System.Random();
                m_RandomizedNotes = m_Notes.OrderBy(x => random.Next()).ToArray();
            }

            return m_RandomizedNotes;
        }
    }
}