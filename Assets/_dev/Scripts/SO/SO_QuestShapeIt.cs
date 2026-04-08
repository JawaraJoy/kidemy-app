using System;
using System.Linq;
using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestShapeIt", menuName = "EduGame/Quest/ShapeIt")]
    public class SO_QuestShapeIt : SO_Quest
    {
         [Header("Question")]
        [SerializeField] private QuestUtilLabel question;
        [SerializeField] private QuestShapeItCanvas canvas;
        
        [Header("Rule")]
        [SerializeField] private float timer = 0;
        [SerializeField] private bool randomizeChoice;

        public QuestUtilLabel Question => question;
        public QuestShapeItCanvas Canvas => canvas;
        public QuestDragNDropItem[] Items => randomizeChoice ? GetRandomizedChoices() : canvas.Items;
        public float Timer => timer;
        
        [NonSerialized] private QuestDragNDropItem[] randomizedChoices;

        QuestDragNDropItem[] GetRandomizedChoices()
        {
            if(randomizedChoices == null)
            {
                randomizedChoices = new QuestDragNDropItem[canvas.Items.Length]; 

                System.Random random = new System.Random();
                randomizedChoices = canvas.Items.OrderBy(x => random.Next()).ToArray();
            }

            return randomizedChoices;
        }
    }
}