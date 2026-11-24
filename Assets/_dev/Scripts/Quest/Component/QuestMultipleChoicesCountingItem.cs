using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMultipleChoicesCountingItem : QuestMultipleChoicesItem
    {
        [SerializeField] private RectTransform m_Container;

        public override void SetChoice(QuestUtilLabelChoice choice)
        {
            base.SetChoice(choice);
            if (!m_Container) return;
            for (int i = m_Container.childCount - 1; i >= 0; i--)
            {
                var old = m_Container.GetChild(i).gameObject;
                old.SetActive(false);
                Destroy(old);
            }
            if (!choice.Image || !StringHelper.TryParseCount(choice.Text, out int count)) return;
            for (int i = 0; i < count; i++)
            {
                var unit = new GameObject("Item", typeof(RectTransform), typeof(Image));
                unit.transform.SetParent(m_Container, false);
                if (unit.TryGetComponent(out Image image)) image.sprite = choice.Image;
            }
        }
    }
}
