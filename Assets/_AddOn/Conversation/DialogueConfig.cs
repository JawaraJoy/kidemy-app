using EasyTextEffects;
using TMPro;
using UnityEngine;

namespace TextToVoice
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
        public string GetWords()
        {
            string words = string.Empty;
            foreach(WordSetting wordSetting in m_WordSettings)
            {
                words += wordSetting.GetWords();
            }
            return words;
        }
    }
}
