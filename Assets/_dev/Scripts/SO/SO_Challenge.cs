using System;
using EduGame;
using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_Challenge", menuName = "EduGame/Challenge")]
    public class SO_Challenge : ScriptableObject
    {
        [SerializeField] private QuestItem[] quests;

        private int difficulty = 1;

        public QuestItem[] Quests => quests;
        public int Difficulty { get => difficulty; set { difficulty = value; } }
    }

    [Serializable]
    public class QuestItem
    {
        [SerializeField] private bool active = true;
        [SerializeField] private SO_Quest quest;
        [SerializeField] private Quest template;

        [Header("Score (Leave zero to use score from quest)")]
        [SerializeField] private int score;

        public bool Active => active;
        public SO_Quest Quest => quest;
        public Quest Template => template;
        public int Score => score;
    }
}