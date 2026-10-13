using System;
using EduGame;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_Challenge", menuName = "EduGame/Challenge")]
    public class SO_Challenge : ScriptableObject
    {
        [SerializeField] [FormerlySerializedAs("quests")] private QuestItem[] m_Quests;

        private int m_Difficulty = 1;

        public QuestItem[] Quests => m_Quests;
        public int Difficulty { get => m_Difficulty; set { m_Difficulty = value; } }
    }

    [Serializable]
    public class QuestItem
    {
        [SerializeField] [FormerlySerializedAs("active")] private bool m_Active = true;
        [SerializeField] [FormerlySerializedAs("quest")] private SO_Quest m_Quest;
        [SerializeField] [FormerlySerializedAs("template")] private Quest m_Template;

        [Header("Score (Leave zero to use score from quest)")]
        [SerializeField] [FormerlySerializedAs("score")] private int m_Score;

        public bool Active => m_Active;
        public SO_Quest Quest => m_Quest;
        public Quest Template => m_Template;
        public int Score => m_Score;
    }
}