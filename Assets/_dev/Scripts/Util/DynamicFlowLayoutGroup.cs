using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Layout/Dynamic Flow Layout Group")]
public class DynamicFlowLayoutGroup : LayoutGroup
{
    public Vector2 cellSize = new Vector2(100, 100);
    public Vector2 spacing = new Vector2(10, 10);

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
    }

    public override void CalculateLayoutInputVertical() { }

    public override void SetLayoutHorizontal() => LayoutChildren();
    public override void SetLayoutVertical() => LayoutChildren();

    private void LayoutChildren()
    {
        float width = rectTransform.rect.width;
        
        // Calculate total available horizontal space after subtracting left and right padding
        float availableWidth = width - padding.left - padding.right;

        // Determine maximum columns that can fit in the padded area
        int columns = Mathf.FloorToInt((availableWidth + spacing.x) / (cellSize.x + spacing.x));
        columns = Mathf.Max(1, columns);

        // Count only active children
        int totalCount = 0;
        for (int i = 0; i < rectChildren.Count; i++)
        {
            if (rectChildren[i].gameObject.activeSelf) totalCount++;
        }

        int currentColumn = 0;
        int currentRow = 0;
        
        // Setup initial row parameters
        int itemsInCurrentRow = Mathf.Min(columns, totalCount);
        float rowWidth = (itemsInCurrentRow * cellSize.x) + ((itemsInCurrentRow - 1) * spacing.x);
        
        // Center the row relative to the padded bounds
        float startX = padding.left + (availableWidth - rowWidth) / 2f;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            var child = rectChildren[i];
            if (!child.gameObject.activeSelf) continue;

            // Handle row wrapping
            if (currentColumn >= columns)
            {
                currentColumn = 0;
                currentRow++;
                
                int remainingItems = totalCount - (currentRow * columns);
                itemsInCurrentRow = Mathf.Min(columns, remainingItems);
                rowWidth = (itemsInCurrentRow * cellSize.x) + ((itemsInCurrentRow - 1) * spacing.x);
                
                // Recalculate centering for the new row
                startX = padding.left + (availableWidth - rowWidth) / 2f;
            }

            // Calculate precise positions accounting for paddings and spacing matrix
            float xPos = startX + (currentColumn * (cellSize.x + spacing.x));
            float yPos = padding.top + (currentRow * (cellSize.y + spacing.y));

            SetChildAlongAxis(child, 0, xPos, cellSize.x);
            SetChildAlongAxis(child, 1, yPos, cellSize.y);

            currentColumn++;
        }
    }
}