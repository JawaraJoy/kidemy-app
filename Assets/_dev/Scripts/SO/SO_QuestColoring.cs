using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestColoring", menuName = "EduGame/Quest/Coloring")]
    public class SO_QuestColoring : SO_Quest
    {
        [Header("Question")]
        [SerializeField] [FormerlySerializedAs("text")] private string m_Text;
        [SerializeField] [FormerlySerializedAs("canvas")] private RectTransform m_Canvas;

        [Header("Rule")]
        //[SerializeField] private int attempt = 10;
        //[SerializeField] private float timer = 0;
        
        public string Text => m_Text;
        public RectTransform Canvas => m_Canvas;
    }
}