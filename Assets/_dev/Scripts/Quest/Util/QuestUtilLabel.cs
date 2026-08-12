using AddOn.TextAnimation;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    [Serializable]
    public class QuestUtilLabel
    {
        [SerializeField] private Sprite image;
        [SerializeField] private string text;
        [SerializeField]
        private DialogueConfig m_FormatedText;
        [SerializeField]
        private Color m_ImageColor = Color.white;
        [SerializeField] private Color color = Color.white;
        [SerializeField]
        private bool m_UseTextColor = false;
        [SerializeField]
        private Color m_TextColor = Color.white;
        [SerializeField] private AudioClip audio;

        public Sprite Image => image;
        public string Text => text;
        public AudioClip Audio => audio;
        public Color Color => new Color(color.r, color.g, color.b, 1);
        public Color ImageColor => m_ImageColor;
        public int Id { get; set; }
        public bool UseTextColor => m_UseTextColor;
        public Color TextColor => m_TextColor;
        public DialogueConfig FormatedText => m_FormatedText;

        public void SetAudio(AudioClip clip)
        {
            audio = clip;
        }

        
    }
}
