namespace EduGame
{
    public class QuestMozaic : QuestDragNDrop
    {
        /// <summary>
        /// Mosaic slots describe the expected tile with their label image.  Unlike a
        /// normal drag-and-drop question, several slots may intentionally use the
        /// same image, so item IDs cannot be used to validate the completed mosaic.
        /// </summary>
        protected override int Check()
        {
            int totalAnswers = 0;

            foreach (var slot in slots)
            {
                if (slot == null || slot.DropZone == null || slot.GroupData?.Label == null)
                    continue;

                // Existing mosaic data stores the tile image in the group's first
                // choice. New data may put it directly on the slot label.
                var expectedImage = slot.GroupData.Label.Image;
                if (expectedImage == null && slot.GroupData.Labels != null && slot.GroupData.Labels.Length > 0)
                    expectedImage = slot.GroupData.Labels[0].Image;

                if (expectedImage == null)
                    continue;

                foreach (var item in slot.DropZone.Items)
                {
                    if (item != null && item.ItemData != null &&
                        item.ItemData.Image == expectedImage)
                    {
                        totalAnswers++;
                    }
                }
            }

            return totalAnswers;
        }
    }
}
