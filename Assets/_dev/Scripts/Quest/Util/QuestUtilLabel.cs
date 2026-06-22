using System;
using UnityEngine;

namespace EduGame
{
    [Serializable]
    public class QuestUtilLabel
    {
        [SerializeField] private Sprite image;
        [SerializeField] private string text;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private AudioClip audio;

        public Sprite Image => image;
        public string Text => text;
        public AudioClip Audio => audio;
        public Color Color => new Color(color.r, color.g, color.b, 1);

        public int Id { get; set; }
    }

}
