using System;
using System.Linq;
using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestColoring", menuName = "EduGame/Quest/Coloring")]
    public class SO_QuestColoring : SO_Quest
    {
        [Header("Question")]
        [SerializeField] private string text;
        [SerializeField] private RectTransform canvas;

        [Header("Rule")]
        //[SerializeField] private int attempt = 10;
        //[SerializeField] private float timer = 0;
        
        public string Text => text;
        public RectTransform Canvas => canvas;
    }
}