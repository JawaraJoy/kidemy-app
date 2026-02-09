using System;
using System.Linq;
using UnityEngine;

namespace EduGame
{
    public class SO_Quest : ScriptableObject
    {
        public enum QuestCategory { MathAndLogic, LiteracyAndLanguage, CreativityAndExpression,SocialEmotionalLearning, LifeSkillsAndDiscovery }

        [Header("Identifier")]
        [SerializeField] private string id;
        [SerializeField] private QuestCategory category;

        public string Id => id;
        public QuestCategory Category => category;
    }
}