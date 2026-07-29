using UnityEngine;
using UnityEngine.UI;

namespace Rush
{
    public class QuestView : UIView
    {
        [SerializeField]
        private Image m_Background;
        [SerializeField]
        private Image m_Illustration;

        public void Show(QuestConfig config)
        {
            m_Background.color = config.BaseColor;
            m_Illustration.sprite = config.MainSprite;
            ConfigInternal(config.QuestGroupContentSetting);
            ShowInternal();
        }
    }
}
