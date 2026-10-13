using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[AddComponentMenu("Layout/Dynamic Flow Layout Group")]
public class DynamicFlowLayoutGroup : LayoutGroup
{
    [SerializeField]
    [FormerlySerializedAs("cellSize")]
    private Vector2 m_CellSize = new Vector2(100, 100);
    [SerializeField]
    [FormerlySerializedAs("spacing")]
    private Vector2 m_Spacing = new Vector2(10, 10);

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        UpdateLayoutInput();
    }

    public override void CalculateLayoutInputVertical()
    {
        UpdateLayoutInput();
    }

    // Memberi tahu Canvas/ContentSizeFitter berapa ukuran total yang dibutuhkan layout ini
    private void UpdateLayoutInput()
    {
        int totalCount = GetActiveChildCount();
        if (totalCount == 0) return;

        float availableWidth = rectTransform.rect.width - padding.left - padding.right;
        int columns = Mathf.Max(1, Mathf.FloorToInt((availableWidth + m_Spacing.x) / (m_CellSize.x + m_Spacing.x)));
        int rows = Mathf.CeilToInt((float)totalCount / columns);

        float totalWidth = padding.left + padding.right + (columns * m_CellSize.x) + ((columns - 1) * m_Spacing.x);
        float totalHeight = padding.top + padding.bottom + (rows * m_CellSize.y) + ((rows - 1) * m_Spacing.y);

        SetLayoutInputForAxis(totalWidth, totalWidth, 1, -1, 0);
        SetLayoutInputForAxis(totalHeight, totalHeight, 1, -1, 1);
    }

    public override void SetLayoutHorizontal() => LayoutChildren();
    public override void SetLayoutVertical() => LayoutChildren();

    private int GetActiveChildCount()
    {
        int count = 0;
        for (int i = 0; i < rectChildren.Count; i++)
        {
            if (rectChildren[i].gameObject.activeSelf) count++;
        }
        return count;
    }

    private void LayoutChildren()
    {
        int totalCount = GetActiveChildCount();
        if (totalCount == 0) return;

        float width = rectTransform.rect.width;
        float availableWidth = width - padding.left - padding.right;

        int columns = Mathf.FloorToInt((availableWidth + m_Spacing.x) / (m_CellSize.x + m_Spacing.x));
        columns = Mathf.Max(1, columns);

        int rows = Mathf.CeilToInt((float)totalCount / columns);

        // 1. Hitung total tinggi grid konten
        float totalGridHeight = (rows * m_CellSize.y) + ((rows - 1) * m_Spacing.y);

        // 2. Gunakan GetStartOffset untuk sumbu Y (Vertical: Upper / Middle / Lower)
        float startY = GetStartOffset(1, totalGridHeight);

        int currentColumn = 0;
        int currentRow = 0;

        int itemsInCurrentRow = Mathf.Min(columns, totalCount);
        float rowWidth = (itemsInCurrentRow * m_CellSize.x) + ((itemsInCurrentRow - 1) * m_Spacing.x);

        // Centering Horizontal bawaan script Anda
        float startX = padding.left + (availableWidth - rowWidth) / 2f;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            var child = rectChildren[i];
            if (!child.gameObject.activeSelf) continue;

            if (currentColumn >= columns)
            {
                currentColumn = 0;
                currentRow++;

                int remainingItems = totalCount - (currentRow * columns);
                itemsInCurrentRow = Mathf.Min(columns, remainingItems);
                rowWidth = (itemsInCurrentRow * m_CellSize.x) + ((itemsInCurrentRow - 1) * m_Spacing.x);

                startX = padding.left + (availableWidth - rowWidth) / 2f;
            }

            float xPos = startX + (currentColumn * (m_CellSize.x + m_Spacing.x));
            
            // 3. Gunakan startY menggantikan padding.top
            float yPos = startY + (currentRow * (m_CellSize.y + m_Spacing.y));

            SetChildAlongAxis(child, 0, xPos, m_CellSize.x);
            SetChildAlongAxis(child, 1, yPos, m_CellSize.y);

            currentColumn++;
        }
    }
}