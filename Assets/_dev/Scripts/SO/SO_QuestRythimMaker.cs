using System;
using System.Linq;
using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestRythimMaker", menuName = "EduGame/Quest/RythimMaker")]
    public class SO_QuestRythimMaker : SO_Quest
    {
        [Header("Question")]
        [SerializeField] private QuestUtilLabel question;
        [SerializeField] private float speed = 1f;
        [SerializeField] private QuestRythimNoteItem[] notes;
        
        [Header("Rule")]
        [SerializeField] private float timer = 0;
        [SerializeField] private bool randomizeChoice;

        public QuestUtilLabel Question => question;
        public QuestRythimNoteItem[] Items => randomizeChoice ? GetRandomizedChoices() : notes;
        public float Timer => timer;
        public float Speed => speed;
        
        [NonSerialized] private QuestRythimNoteItem[] randomizedNotes;

        QuestRythimNoteItem[] GetRandomizedChoices()
        {
            if(randomizedNotes == null)
            {
                randomizedNotes = new QuestRythimNoteItem[notes.Length]; 

                System.Random random = new System.Random();
                randomizedNotes = notes.OrderBy(x => random.Next()).ToArray();
            }

            return randomizedNotes;
        }
    }
}