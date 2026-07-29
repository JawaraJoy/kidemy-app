using MoreMountains.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Rush
{
    public class QuestManager : Singleton<QuestManager>
    {
        [SerializeField]
        private PanelConfig m_DialoguePanel;
        [SerializeField]
        private CanvasConfig m_UseCanvasForGameplay;
        [SerializeField]
        private QuestConfig[] m_QuestConfigs;

        [SerializeField, MMReadOnly]
        private List<Quest> m_Quests = new();

        [SerializeField, MMReadOnly]
        private Quest m_CurrentQuestPlaying;
        [SerializeField, MMReadOnly]
        private int m_AllQuestScore = 0;
        [SerializeField, MMReadOnly]
        private int m_ClearedQuestScore = 0;
        [SerializeField]
        private UnityEvent<Quest> m_OnQuestPlayed;
        [SerializeField]
        private UnityEvent<Quest> m_OnCleareQuest;

        private DialoguePresenter m_DialoguePresenter;
        public DialoguePresenter DialoguePresenter => m_DialoguePresenter;
        public void Init()
        {
            foreach (QuestConfig config in m_QuestConfigs)
            {
                RegisterQuestInternal(config);
            }

            if (CanvasManager.Instance.HasCanvas(m_DialoguePanel.AttachedCanvas, out CanvasView canvasView))
            {
                if (canvasView.HasPanel(m_DialoguePanel, out PanelView panel))
                {
                    if (panel.HasExtention(out DialoguePresenter dialoguePresenter))
                    {
                        m_DialoguePresenter = dialoguePresenter;
                    }
                }
            }
        }

        private bool HasQuestInternal(QuestConfig config, out Quest quest)
        {
            quest = null;
            foreach (Quest exist in m_Quests)
            {
                if (exist.QuestConfig.Info.Id == config.Info.Id)
                {
                    quest = exist;
                }
            }
            return quest != null;
        }

        private void RegisterQuestInternal(QuestConfig config)
        {
            if (HasQuestInternal(config, out Quest quest))
            {
                
            }
            else
            {
                Quest newQuest = new Quest(config);
                m_Quests.Add(newQuest);
                AddAllQuestScore(newQuest.QuestConfig.ScoreAmount);
            }
        }

        private void AddAllQuestScore(int score)
        {
            m_AllQuestScore += score;
        }
        private void AddClearedQuestScore(int score)
        {
            m_ClearedQuestScore += score;

            m_ClearedQuestScore = Mathf.Clamp(m_ClearedQuestScore, 0, m_AllQuestScore);
        }

        public void PlayQuest(QuestConfig config)
        {
            if (HasQuestInternal(config, out Quest quest))
            {
                m_CurrentQuestPlaying = quest;
                if (m_CurrentQuestPlaying.State == Quest.QuestState.Locked)
                {
                    m_CurrentQuestPlaying.ChangeState(Quest.QuestState.Unlocked);
                }
                PanelConfig usedPanel = m_CurrentQuestPlaying.QuestConfig.PanelUsed;
                if (CanvasManager.Instance.HasCanvas(usedPanel.AttachedCanvas, out CanvasView canvasView))
                {
                    if (canvasView.HasPanel(usedPanel, out PanelView panel))
                    {
                        if (panel.HasExtention(out QuestPresenter questPresenter))
                        {
                            questPresenter.Show(m_CurrentQuestPlaying);
                        }
                    }
                }

            }
        }
        public void ClearQuest(Quest quest)
        {
            if (m_CurrentQuestPlaying.State != Quest.QuestState.Cleared)
            {
                m_CurrentQuestPlaying.ChangeState(Quest.QuestState.Cleared);
                AddClearedQuestScore(m_CurrentQuestPlaying.Scored);
                m_CurrentQuestPlaying.AddScore(quest.QuestConfig.ScoreAmount);
            }
            m_OnCleareQuest?.Invoke(quest);
        }
    }
}
