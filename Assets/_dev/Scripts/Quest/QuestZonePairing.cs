
namespace EduGame
{
    public abstract class QuestZonePairing : Quest
    {
        public virtual bool Verify( QuestDragNDropItem item, QuestDragNDropZone zone)
        {
            return true;
        }
    }
}