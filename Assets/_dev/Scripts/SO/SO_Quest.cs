using System;
using System.Linq;
using UnityEngine;

namespace EduGame
{
    public abstract class SO_Quest : ScriptableObject
    {
        public enum QuestCategory { MATH_AND_LOGIC, LITERACY_AND_LANGUAGE, CREATIVITY_AND_EXPRESSION,SOCIAL_EMOTIONAL_LEARNING, LIFE_SKILLS_AND_DISCOVERY }

        [Header("Identifier")]
        [SerializeField] protected string id;
        [SerializeField] protected string title;
        [SerializeField] protected QuestCategory category;

        [Header("Rule")]
        [SerializeField] protected bool autoSubmit;
        [SerializeField] private int score = 10;
        [SerializeField] private float tresholdTime = 20;


        public string Id => id;
        public string Title => title;
        public QuestCategory Category => category;
        public bool AutoSubmit => autoSubmit;
        public int Score => score;
        public float TresholdTime => tresholdTime;
    }
}