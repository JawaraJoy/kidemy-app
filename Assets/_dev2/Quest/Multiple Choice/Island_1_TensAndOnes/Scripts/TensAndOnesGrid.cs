using UnityEngine;
using UnityEngine.UI;

namespace Rush
{
    [DisallowMultipleComponent]
    public sealed class TensAndOnesGrid : GridLayoutGroup
    {
        [SerializeField, Min(0.1f)] private float m_CardAspectRatio = 2.02f;

        public override void SetLayoutHorizontal()
        {
            FitCardsInternal();
            base.SetLayoutHorizontal();
        }

        public override void SetLayoutVertical()
        {
            FitCardsInternal();
            base.SetLayoutVertical();
        }

        private void FitCardsInternal()
        {
            Vector2 size = rectTransform.rect.size;
            int columns = constraintCount;
            float totalSpacing = spacing.x * (columns - 1);
            float width = Mathf.Max(0f, (size.x - padding.horizontal - totalSpacing) / columns);
            float height = Mathf.Max(0f, size.y - padding.vertical);
            float cardHeight = Mathf.Min(height, width / m_CardAspectRatio);
            m_CellSize = new Vector2(cardHeight * m_CardAspectRatio, cardHeight);
        }
    }
}
