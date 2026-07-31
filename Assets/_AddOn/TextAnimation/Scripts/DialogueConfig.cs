using EasyTextEffects;
using TMPro;
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

        public Sprite Potrait => m_Potrait;
        public TagEffectsPreset EffectPreset => m_EffectPreset;
        public string GetFormatedText()
        {
            string words = string.Empty;
            foreach(WordSetting wordSetting in m_WordSettings)
            {
                words += wordSetting.GetFormatedText();
            }
            return words;
        }
        public string GetPlainText()
        {
            string text = string.Empty;
            foreach(WordSetting wordSetting in m_WordSettings)
            {
                text += wordSetting.GetPlainText();
            }
            return text;
        }

    }
}
