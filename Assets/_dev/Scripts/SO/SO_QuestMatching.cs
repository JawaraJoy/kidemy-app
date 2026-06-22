using System;
using System.Linq;
using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestMatching", menuName = "EduGame/Quest/Matching")]
    public class SO_QuestMatching : SO_Quest
    {
        [Header("Question")]
        [SerializeField] private string text;
        [SerializeField] private QuestUtilLabelMatching[] matches;

        [Header("Rule")]
        //[SerializeField] private int attempt = 10;
        //[SerializeField] private float timer = 0;

        public string Text => text;
        public QuestUtilLabelMatching[] Matches => GetMatches();

        [NonSerialized] private QuestUtilLabelMatching[] compiledMatches;
        
        QuestUtilLabelMatching[] GetMatches()
        {
            if(compiledMatches == null)
            {
                compiledMatches = new QuestUtilLabelMatching[matches.Length * 2];
                
                for (int i = 0; i < matches.Length; i++)
                {
                    matches[i].SetId(i);
                    compiledMatches[i * 2] =  matches[i];
                    compiledMatches[i * 2 + 1] =  matches[i];
                }
                    
                System.Random random = new System.Random();
                compiledMatches = compiledMatches.OrderBy(x => random.Next()).ToArray();
            }

            return compiledMatches;
        }
    }
}