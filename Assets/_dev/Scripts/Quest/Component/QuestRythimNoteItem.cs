using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    [Serializable]
    public class QuestRythimNoteItem
    {   
        [SerializeField] [FormerlySerializedAs("notePrefab")] protected QuestRythimNote m_NotePrefab;
        [SerializeField] [FormerlySerializedAs("delay")] protected float m_Delay;
        [SerializeField] [FormerlySerializedAs("speedModifier")] protected float m_SpeedModifier = 1;
        [SerializeField] [FormerlySerializedAs("clip")] protected AudioClip m_Clip;
        
        public QuestRythimNote Note => m_NotePrefab;
        public float Delay => m_Delay;
        public float SpeedModifier => m_SpeedModifier;
        public AudioClip Clip => m_Clip;
    }
}