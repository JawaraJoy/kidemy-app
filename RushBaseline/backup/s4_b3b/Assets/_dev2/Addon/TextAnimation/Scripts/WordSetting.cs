using EasyTextEffects.Effects;
using UnityEngine;

namespace AddOn.TextAnimation
{
    [System.Serializable]
    public class WordSetting
    {
        [SerializeField, TextArea]
        private string m_Words = "Hello";
        [SerializeField]
        private bool m_AddSpaceForNext;
        
        [SerializeField]
        private TextEffectInstance[] m_Effects;

        private const string c_StartFormat = "<link=";
        private const string c_AddEffect = "+";
        private const string c_EndEffect = ">";
        private const string c_EndFormat = "</link>";

        public string GetFormattedText()
        {
            string wordEffects = WordSetting.c_StartFormat;
            for (int i = 0; i < m_Effects.Length; i++)
            {
                string tag = m_Effects[i].effectTag;
                if (i > 0)
                {
                    wordEffects += $"{WordSetting.c_AddEffect}{tag}";
                }
                else
                {
                    wordEffects += tag;
                }
                
            }
            
            wordEffects += $"{WordSetting.c_EndEffect}{m_Words}{WordSetting.c_EndFormat}";
            if (m_AddSpaceForNext)
            {
                wordEffects += " ";
            }
            return wordEffects;
        }
        public string GetPlainText()
        {
            return m_AddSpaceForNext? m_Words + " " : m_Words;
        }

        public static string GetFormatedText(string text, TextEffectInstance[] textEffects)
        {
            string wordEffects = WordSetting.c_StartFormat;
            for (int i = 0; i < textEffects.Length; i++)
            {
                string tag = textEffects[i].effectTag;
                if (i > 0)
                {
                    wordEffects += $"{WordSetting.c_AddEffect}{tag}";
                }
                else
                {
                    wordEffects += tag;
                }

            }

            wordEffects += $"{WordSetting.c_EndEffect}{text}{WordSetting.c_EndFormat}";
            return wordEffects;
        }
    }
}
