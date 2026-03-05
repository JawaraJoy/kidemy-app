using System;
using System.Linq;
using UnityEngine;

namespace EduGame
{
    public abstract class SO_Quest : ScriptableObject
    {
        public enum QuestCategory { MathAndLogic, LiteracyAndLanguage, CreativityAndExpression,SocialEmotionalLearning, LifeSkillsAndDiscovery }

        [Header("Identifier")]
        [SerializeField] protected string id;
        [SerializeField] protected QuestCategory category;

        [Header("Rule")]
        [SerializeField] protected bool autoSubmit;
        [SerializeField] private int score = 10;


        public string Id => id;
        public QuestCategory Category => category;
        public bool AutoSubmit => autoSubmit;
        public int Score => score;
    }
}