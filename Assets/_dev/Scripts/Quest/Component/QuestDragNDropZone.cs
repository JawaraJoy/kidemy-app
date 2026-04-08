using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestDragNDropZone : MonoBehaviour, IDropHandler
    {
        public enum DisplayMode { Normal, Fill, ScaledUp, ScaledDown }

        [SerializeField] private DisplayMode displayMode;

        private QuestDragNDropItem[] items;
        private QuestDragNDropSlot slot;

        public QuestDragNDropItem[] Items => items;
        public DisplayMode CurrentDisplayMode => displayMode;

        void Start()
        {
            slot = GetComponentInParent<QuestDragNDropSlot>();

            RegisterItems();
        }

        public void RegisterItems()
        {
            items = new QuestDragNDropItem[transform.childCount];

            int x = 0;

            foreach (Transform child in transform)
            {
                items[x] = child.GetComponent<QuestDragNDropItem>();
                x++;
            }
        }

        int GetItemIndex(QuestDragNDropItem seekItem)
        {
            int i = 0;

            if (items != null)
            {
                foreach (var item in items)
                {
                    if (seekItem.transform.GetInstanceID() != item.transform.GetInstanceID())
                        i++;
                    else
                        break;
                }
            }

            return i;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null)
            {
                QuestDragNDropItem draggedItem = eventData.pointerDrag.GetComponent<QuestDragNDropItem>();

                if (draggedItem)
                {
                    bool slotLimitVerification = !slot || (slot.GroupData.Limit < 0 || (items.Length < slot.GroupData.Limit));

                    if (slotLimitVerification)
                    {
                        int itemIndex = items.Length;
                        bool isSameZone = false;

                        if (draggedItem.OriginalZone.GetInstanceID() == transform.GetInstanceID())
                        {
                            itemIndex = GetItemIndex(draggedItem);
                            isSameZone = true;
                        }

                        draggedItem.transform.parent = transform;
                        draggedItem.transform.SetSiblingIndex(itemIndex);

                        if (!isSameZone)
                        {
                            draggedItem.OriginalZone.RegisterItems();
                            draggedItem.RegisterZone();
                        }

                        if (displayMode == DisplayMode.Fill)
                            FillZone(draggedItem);
                        else if (displayMode == DisplayMode.ScaledUp)
                            Scaled(draggedItem, 2);
                        else if (displayMode == DisplayMode.ScaledDown)
                            Scaled(draggedItem, 0.5f);

                        RegisterItems();
                    }
                    else
                    {
                        if (draggedItem.OriginalZone)
                            draggedItem.OriginalZone.ReInsertItem(draggedItem);
                    }
                }
            }
        }

        public void ReInsertItem(QuestDragNDropItem item)
        {
            int itemIndex = GetItemIndex(item);
            item.transform.parent = transform;
            item.transform.SetSiblingIndex(itemIndex);

            if (displayMode == DisplayMode.Fill)
                FillZone(item);
            else if (displayMode == DisplayMode.ScaledUp)
                Scaled(item, 2);
            else if (displayMode == DisplayMode.ScaledDown)
                Scaled(item, 0.5f);

            RegisterItems();
        }

        public void FillZone(QuestDragNDropItem item)
        {
            item.Rect.anchorMax = Vector2.one;
            item.Rect.anchorMin = Vector2.zero;

            item.Rect.pivot = Vector2.one * 0.5f;

            item.Rect.offsetMin = Vector2.zero;
            item.Rect.offsetMax = Vector2.zero;
        }

        public void Scaled(QuestDragNDropItem item, float scale)
        {
            Debug.Log(scale);
            RectTransform rect = item.transform.GetChild(0).GetComponent<RectTransform>();

            rect.pivot = Vector2.one * 0.5f;
            rect.anchorMax = Vector2.one * 0.5f;
            rect.anchorMin = Vector2.one * 0.5f;

            rect.anchoredPosition = Vector2.zero;

            rect.localScale = Vector3.one * scale;
        }
    }
}