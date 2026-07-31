using EasyTextEffects;
using System.Text;
using UnityEngine;

namespace AddOn.TextAnimation
{
    [CreateAssetMenu(fileName = "Dialogue_", menuName = "AddOn/TextToVoice/Dialogue")]
    public class DialogueConfig : Config
    {
        [SerializeField]
        private Sprite m_Potrait;
        [SerializeField]
        private TagEffectsPreset m_EffectPreset;
        [SerializeField]
        private WordSetting[] m_WordSettings;
        [SerializeField]
        private AudioClip m_VoiceClip; // generate audio to this

        public Sprite Potrait => m_Potrait;
        public TagEffectsPreset EffectPreset => m_EffectPreset;
        public AudioClip VoiceClip => m_VoiceClip;
        public string GetFormattedText() // use this to show in UI
        {
            StringBuilder builder = new();

            foreach (WordSetting wordSetting in m_WordSettings)
            {
                builder.Append(wordSetting.GetFormattedText());
            }

            return builder.ToString();
        }
        public string GetPlainText() // use this to generate to audio
        {
            StringBuilder builder = new();

            foreach (WordSetting wordSetting in m_WordSettings)
            {
                builder.Append(wordSetting.GetPlainText());
            }

            return builder.ToString();
        }

    }
}
