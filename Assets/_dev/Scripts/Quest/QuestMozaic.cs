namespace EduGame
{
    public class QuestMozaic : QuestDragNDrop
    {
        protected override bool MatchesSlot(QuestDragNDropItem item, QuestDragNDropSlot slot)
        {
            if (!item || item.ItemData == null || !slot || slot.GroupData == null) return false;
            var group = slot.GroupData;
            var image = group.Label?.Image;
            if (!image && group.Labels?.Length > 0) image = group.Labels[0]?.Image;
            return image && item.ItemData.Image == image;
        }
    }
}
