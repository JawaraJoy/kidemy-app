using UnityEngine;
using UnityEngine.UI;

namespace Rush
{
    public class KeyView : UIView
    {
        [SerializeField]
        private Image m_Image;
        private QuestKeyConfig m_KeyConfig;
        [SerializeField]
        private Button m_AnswerButton;
        public Button AnswerButton => m_AnswerButton;
        public QuestKeyConfig KeyConfig => m_KeyConfig;

        private void Config(QuestKeyConfig keyConfig)
        {
            m_KeyConfig = keyConfig;

            m_Image.sprite = keyConfig.MainSprite;
            m_Image.color = keyConfig.BaseColor;

            keyConfig.Config(this);
        }
        public void Show(QuestKeyConfig config)
        {
            Config(config);
            ShowInternal();
        }
    }
}
