using MoreMountains.Tools;
using System.Collections.Generic;
using UnityEngine;

namespace Rush
{
    [System.Serializable]
    public class Quest
    {
        public enum QuestState
        {
            Locked,
            Unlocked,
            Unclear,
            Cleared,
        }
        [SerializeField, MMReadOnly]
        private QuestState m_State = QuestState.Locked;
        [SerializeField, MMReadOnly]
        private int m_Scored = 0;
        [SerializeField]
        private List<QuestKeyConfig> m_KeySelecteds = new();
        [SerializeField, MMReadOnly]
        private QuestConfig m_QuestConfig;
        [SerializeField, MMReadOnly]
        private PanelConfig m_UsedPanel;
        public QuestConfig QuestConfig => m_QuestConfig;
        public QuestState State => m_State;
        public int Scored => m_Scored;
        public List<QuestKeyConfig> KeySelecteds => m_KeySelecteds;

        public Quest (QuestConfig questConfig)
        {
            m_QuestConfig = questConfig;
        }
        private void AddKeyInternal(QuestKeyConfig key)
        {
            if (!m_KeySelecteds.Contains(key))
            {
                m_KeySelecteds.Add(key);
            }
        }

        public void AddKey(QuestKeyConfig key)
        {
            AddKeyInternal(key);
        }

        public void Answer(QuestKeyConfig answer)
        {
            m_QuestConfig.Answer(answer, this);
        }
        private void ChangeStateInternal(QuestState state)
        {
            m_State = state;
        }
        public void ChangeState(QuestState state)
        {
            ChangeStateInternal(state);
        }
        public void AddScore(int score)
        {
            m_Scored += score;
        }
    }
}
