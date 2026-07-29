using System.Linq;
using UnityEngine;

namespace Rush
{
    public abstract class QuestConfig : Config
    {
        [SerializeField]
        private PanelConfig m_PanelUsed;
        [SerializeField]
        private QuestCategoryConfig m_Category;
        [SerializeField]
        private int m_ScoreAmount;

        [SerializeField]
        private KeyView m_KeyViewPrefab;
        [SerializeField]
        protected QuestKeyConfig[] m_QuestKeys;
        [SerializeField]
        protected QuestKeyConfig[] m_RightKeys;
        public enum ClearCondition
        {
            SelectedKeysEqualToRightKeys,         // Isi sama, urutan bebas
            SelectedKeysEqualToRightKeysInOrder,  // Isi dan urutan sama
            SelectedKeysContainAnyRightKey,       // Minimal satu benar
        }
        [SerializeField]
        private ClearCondition m_ClearCondition;
        public enum DialogueMode
        {
            CharacterOnLeft,
            CharacterOnRight,
        }

        [SerializeField]
        private DialogueMode m_DialogueMode = DialogueMode.CharacterOnLeft;

        [SerializeField]
        private UISettingConfig m_QuestPanelContentSetting;
        [SerializeField]
        private UISettingConfig m_QuestGroupContentSetting;
        [SerializeField]
        private UISettingConfig m_KeyGroupContentSetting;

        [SerializeField]
        private DialogueConfig m_DialogueConfig;

        public int ScoreAmount => m_ScoreAmount;

        public UISettingConfig QuestPanelContentSetting => m_QuestPanelContentSetting;
        public UISettingConfig QuestGroupContentSetting => m_QuestGroupContentSetting;
        public UISettingConfig KeyGroupContentSetting => m_KeyGroupContentSetting;
        public PanelConfig PanelUsed => m_PanelUsed;
        public KeyView KeyViewPrefab => m_KeyViewPrefab;
        public QuestKeyConfig[] RightKeys => m_RightKeys;
        public QuestKeyConfig[] QuestKeys => m_QuestKeys;
        public DialogueConfig DialogueConfig => m_DialogueConfig;
        public virtual void Answer(QuestKeyConfig key, Quest quest)
        {
            quest.AddKey(key);
            if (quest.State == Quest.QuestState.Unlocked)
            {
                quest.ChangeState(Quest.QuestState.Unclear);
            }
            if (IsClear(quest))
            {
                QuestManager.Instance.ClearQuest(quest);
            }
        }

        protected virtual bool IsClear(Quest quest)
        {
            switch (m_ClearCondition)
            {
                case ClearCondition.SelectedKeysEqualToRightKeys:
                    return IsSelectedKeysEqualToRightKeys(quest);

                case ClearCondition.SelectedKeysEqualToRightKeysInOrder:
                    return IsSelectedKeysEqualToRightKeysInOrder(quest);

                case ClearCondition.SelectedKeysContainAnyRightKey:
                    return IsSelectedKeysContainAnyRightKey(quest);

                default:
                    return false;
            }
        }

        private bool IsSelectedKeysEqualToRightKeys(Quest quest)
        {
            if (quest.KeySelecteds.Count != m_RightKeys.Length)
                return false;

            return !quest.KeySelecteds.Except(m_RightKeys).Any() && !m_RightKeys.Except(quest.KeySelecteds).Any();
        }

        private bool IsSelectedKeysEqualToRightKeysInOrder(Quest quest)
        {
            if (quest.KeySelecteds.Count != m_RightKeys.Length)
                return false;

            return quest.KeySelecteds.SequenceEqual(m_RightKeys);
        }

        private bool IsSelectedKeysContainAnyRightKey(Quest quest)
        {
            return quest.KeySelecteds.Any(selected => m_RightKeys.Contains(selected));
        }
    }
}
