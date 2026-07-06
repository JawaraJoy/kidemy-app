using System;
using UnityEngine;

namespace EduGame
{
    [Serializable]
    public class QuestRythimNoteItem
    {   
        [SerializeField] protected QuestRythimNote notePrefab;
        [SerializeField] protected float delay;
        [SerializeField] protected float speedModifier = 1;
        [SerializeField] protected AudioClip clip;
        
        public QuestRythimNote Note => notePrefab;
        public float Delay => delay;
        public float SpeedModifier => speedModifier;
        public AudioClip Clip => clip;
    }
}