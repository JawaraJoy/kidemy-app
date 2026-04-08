using System;
using UnityEngine;

namespace EduGame
{
    [Serializable]
    public class QuestUtilLabel
    {
        [SerializeField] private Sprite image;
        [SerializeField] private string text;
        [SerializeField] private AudioClip audio;

        public Sprite Image => image;
        public string Text => text;
        public AudioClip Audio => audio;

        public int Id { get; set; }
    }

}
