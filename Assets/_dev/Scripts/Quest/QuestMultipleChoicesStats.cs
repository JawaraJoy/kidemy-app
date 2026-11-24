using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMultipleChoicesStats : QuestMultipleChoices
    {
        [SerializeField, FormerlySerializedAs("statPanels")] private RectTransform[] m_StatPanels;

        protected override void Start()
        {
            base.Start();
            if (!(Data is SO_QuestMultipleChoiceStats question) || m_StatPanels == null) return;
            for (int i = 0; i < m_StatPanels.Length; i++)
            {
                var panel = m_StatPanels[i];
                if (!panel) continue;
                var stat = question.Stats != null && i < question.Stats.Length ? question.Stats[i] : null;
                int count = 0;
                bool visible = stat != null && StringHelper.TryParseCount(stat.Text, out count);
                panel.gameObject.SetActive(visible);
                if (!visible) continue;
                for (int j = 0; j < count; j++)
                {
                    var unit = new GameObject("Unit", typeof(RectTransform), typeof(Image));
                    unit.transform.SetParent(panel, false);
                    if (unit.TryGetComponent(out Image image)) image.sprite = stat.Image;
                }
            }
        }
    }
}
