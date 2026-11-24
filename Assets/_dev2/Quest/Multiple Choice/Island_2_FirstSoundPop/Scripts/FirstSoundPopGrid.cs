using UnityEngine;
using UnityEngine.UI;

namespace Rush
{
    public sealed class FirstSoundPopGrid : GridLayoutGroup
    {
        [SerializeField, Min(0.1f)] private float m_CardAspect = 1.43f;

        public override void SetLayoutHorizontal()
        {
            FitCards();
            base.SetLayoutHorizontal();
        }

        public override void SetLayoutVertical()
        {
            FitCards();
            base.SetLayoutVertical();
        }

        private void FitCards()
        {
            int columns = constraintCount;
            int rows = Mathf.Max(1, Mathf.CeilToInt((float)rectChildren.Count / columns));
            Vector2 available = rectTransform.rect.size;
            float width = (available.x - padding.horizontal - spacing.x * (columns - 1)) / columns;
            float height = (available.y - padding.vertical - spacing.y * (rows - 1)) / rows;
            float cardHeight = Mathf.Max(0f, Mathf.Min(height, width / m_CardAspect));
            m_CellSize = new Vector2(cardHeight * m_CardAspect, cardHeight);
        }
    }
}
