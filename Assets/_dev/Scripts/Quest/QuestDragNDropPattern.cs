using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestDragNDropPattern : QuestDragNDrop
    {
        [SerializeField, FormerlySerializedAs("slotPatterns")] private RectTransform[] m_SlotPatterns;

        protected override RectTransform SlotParent(int index)
        {
            return m_SlotPatterns != null && index < m_SlotPatterns.Length ? m_SlotPatterns[index] : null;
        }
    }
}
