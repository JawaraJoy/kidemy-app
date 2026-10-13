using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestMatching", menuName = "EduGame/Quest/Matching")]
    public class SO_QuestMatching : SO_Quest
    {
        [Header("Question")]
        [SerializeField] [FormerlySerializedAs("text")] private string m_Text;
        [SerializeField] [FormerlySerializedAs("matches")] private QuestUtilLabelMatching[] m_Matches;

        [Header("Rule")]
        //[SerializeField] private int attempt = 10;
        //[SerializeField] private float timer = 0;

        public string Text => m_Text;
        public QuestUtilLabelMatching[] Matches => GetMatches();

        [NonSerialized] private QuestUtilLabelMatching[] m_CompiledMatches;
        
        QuestUtilLabelMatching[] GetMatches()
        {
            if(m_CompiledMatches == null)
            {
                m_CompiledMatches = new QuestUtilLabelMatching[m_Matches.Length * 2];
                
                for (int i = 0; i < m_Matches.Length; i++)
                {
                    m_Matches[i].SetId(i);
                    m_CompiledMatches[i * 2] =  m_Matches[i];
                    m_CompiledMatches[i * 2 + 1] =  m_Matches[i];
                }
                    
                System.Random random = new System.Random();
                m_CompiledMatches = m_CompiledMatches.OrderBy(x => random.Next()).ToArray();
            }

            return m_CompiledMatches;
        }
    }
}