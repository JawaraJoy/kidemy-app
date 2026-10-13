using AddOn.TextAnimation;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    [Serializable]
    public class QuestUtilLabel
    {
        [SerializeField] [FormerlySerializedAs("image")] private Sprite m_Image;
        [SerializeField] [FormerlySerializedAs("text")] private string m_Text;
        [SerializeField]
        private DialogueConfig m_FormatedText;
        [SerializeField]
        private Color m_ImageColor = Color.white;
        [SerializeField] [FormerlySerializedAs("color")] private Color m_Color = Color.white;
        [SerializeField]
        private bool m_UseTextColor = false;
        [SerializeField]
        private Color m_TextColor = Color.white;
        [SerializeField] [FormerlySerializedAs("audio")] private AudioClip m_Audio;

        public Sprite Image => m_Image;
        public string Text => m_Text;
        public AudioClip Audio => m_Audio;
        public Color Color => new Color(m_Color.r, m_Color.g, m_Color.b, 1);
        public Color ImageColor => m_ImageColor;
        public int Id { get; set; }
        public bool UseTextColor => m_UseTextColor;
        public Color TextColor => m_TextColor;
        public DialogueConfig FormatedText => m_FormatedText;

        public void SetAudio(AudioClip clip)
        {
            m_Audio = clip;
        }

        
    }
}
