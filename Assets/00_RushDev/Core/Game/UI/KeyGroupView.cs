using MoreMountains.Tools;
using System.Collections.Generic;
using UnityEngine;

namespace Rush
{
    public class KeyGroupView : UIView
    {
        [SerializeField, MMReadOnly]
        private List<KeyView> m_KeyViews = new();

        public void Show(Quest quest)
        {
            Config(quest);
            ShowInternal();
        }
        private void Config(Quest quest)
        {
            ConfigInternal(quest.QuestConfig.KeyGroupContentSetting);

            QuestKeyConfig[] questKeys = quest.QuestConfig.QuestKeys;
            // Spawn jika jumlah KeyView kurang
            while (m_KeyViews.Count < questKeys.Length)
            {
                KeyView spawn = Instantiate(quest.QuestConfig.KeyViewPrefab, m_Content.transform, false);

                m_KeyViews.Add(spawn);
            }

            // Konfigurasi semua yang digunakan
            for (int i = 0; i < questKeys.Length; i++)
            {
                KeyView keyView = m_KeyViews[i];

                keyView.Show(questKeys[i]);

                keyView.AnswerButton.onClick.RemoveAllListeners();
                keyView.AnswerButton.onClick.AddListener(() => Answer(keyView.KeyConfig, quest));
            }

            // Sembunyikan yang tidak dipakai
            for (int i = questKeys.Length; i < m_KeyViews.Count; i++)
            {
                m_KeyViews[i].Hide();
            }
        }
        private void Answer(QuestKeyConfig keyConfig, Quest quest)
        {
            quest.Answer(keyConfig);
        }
    }
}
