using UnityEngine;

namespace Rush
{
    public class QuestPresenter : UIView
    {
        [SerializeField]
        private QuestView m_QuestView;
        [SerializeField]
        private KeyGroupView m_KeyGroupView;
        [SerializeField]
        private DialogueView m_DialogueView;

        private Quest m_Quest;

        public void Show(Quest quest)
        {
            m_Quest = quest;

            ConfigInternal(m_Quest.QuestConfig.QuestPanelContentSetting);

            m_QuestView.Show(quest.QuestConfig);
            m_KeyGroupView.Show(m_Quest);

            ShowInternal();
        }
    }
}
