using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestDragNDropZone : MonoBehaviour, IDropHandler
    {
        public enum DisplayMode { Normal, Dynamic, Sticky, Fill, ScaledUp, ScaledDown, ScaledDown2, FollowParent }

        [SerializeField] private DisplayMode displayMode;
        [SerializeField] private TMP_Text dropInfo;
        [SerializeField] private bool verify;
        [SerializeField] private bool answer;

        [Header("Feedback")]
        [SerializeField] private AudioClip acceptSound;
        [SerializeField] private AudioClip rejectSound;

        private QuestDragNDropItem[] items;
        private QuestDragNDropSlot slot;
        private QuestZonePairing quest;
        private GridLayoutGroup grid;
        private Animator animator;

        public QuestDragNDropItem[] Items => items;
        public DisplayMode CurrentDisplayMode => displayMode;

        void Awake()
        {
            slot = GetComponentInParent<QuestDragNDropSlot>();

            quest = GetComponentInParent<QuestZonePairing>();

            animator = GetComponent<Animator>();
            grid = GetComponentInChildren<GridLayoutGroup>();

            RegisterItems();
        }

        public void AddItem(QuestDragNDropItem item)
        {
            if (item)
            {
                int itemIndex = items.Length;
                bool isSameZone = false;

                if (item.OriginalZone && item.OriginalZone.GetInstanceID() == transform.GetInstanceID())
                {
                    itemIndex = GetItemIndex(item);
                    isSameZone = true;
                }

                item.transform.SetParent(transform);
                item.transform.SetSiblingIndex(itemIndex);

                if (!isSameZone)
                {
                    if(item.OriginalZone)
                        item.OriginalZone.RegisterItems();

                    item.RegisterZone();
                }

                if (displayMode == DisplayMode.Fill)
                    FillZone(item);
                else if (displayMode == DisplayMode.Dynamic)
                    Dynamic(item);
                else if (displayMode == DisplayMode.Sticky)
                    Sticky(item);
                else if (displayMode == DisplayMode.ScaledUp)
                    Scaled(item, 2);
                else if (displayMode == DisplayMode.ScaledDown)
                    Scaled(item, 0.5f);
                else if (displayMode == DisplayMode.ScaledDown2)
                    Scaled(item, 0.75f);
                else if (displayMode == DisplayMode.FollowParent)
                    FollowParent(item);
                else if (displayMode == DisplayMode.Normal)
                    Normal(item);

                RegisterItems();
            }
        }

        public void RegisterItems()
        {
            items = new QuestDragNDropItem[transform.childCount];

            int x = 0;

            if(transform.childCount > 0)
            {
                foreach (Transform child in transform)
                {
                    items[x] = child.GetComponent<QuestDragNDropItem>();
                    x++;
                }
                
                if(dropInfo)
                    dropInfo.gameObject.SetActive(false);
            }
            else
            {
                if(dropInfo)
                    dropInfo.gameObject.SetActive(true);
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
            Debug.Log(eventData.pointerDrag);

            if (eventData.pointerDrag != null)
            {
                QuestDragNDropItem draggedItem = eventData.pointerDrag.GetComponent<QuestDragNDropItem>();

                if (draggedItem)
                {
                    bool slotLimitVerification = !slot || (slot.GroupData.Limit < 0 || (items.Length < slot.GroupData.Limit));

                    if (slotLimitVerification && (!verify || quest.Verify(draggedItem, this)))
                    {
                        AddItem(draggedItem);

                        if(answer)
                            quest.OnAnswered(true);

                        OnAccept();
                    }
                    else
                    {
                        OnReject();

                        if (draggedItem.OriginalZone)
                            draggedItem.OriginalZone.ReInsertItem(draggedItem);
                    }
                }
            }
        }

        public void ReInsertItem(QuestDragNDropItem item)
        {
            int itemIndex = GetItemIndex(item);
            item.transform.SetParent(transform);
            item.transform.SetSiblingIndex(itemIndex);

            if (displayMode == DisplayMode.Fill)
                FillZone(item);
            else if (displayMode == DisplayMode.Dynamic)
                Dynamic(item);
            else if (displayMode == DisplayMode.Sticky)
                Sticky(item);
            else if (displayMode == DisplayMode.ScaledUp)
                Scaled(item, 2);
            else if (displayMode == DisplayMode.ScaledDown)
                Scaled(item, 0.5f);
            else if (displayMode == DisplayMode.ScaledDown2)
                Scaled(item, 0.75f);
            else if (displayMode == DisplayMode.FollowParent)
                FollowParent(item);
            else if (displayMode == DisplayMode.Normal)
                Normal(item);

            RegisterItems();
        }

        public void Dynamic(QuestDragNDropItem item)
        {
            RectTransform rect = item.transform.GetChild(0).GetComponent<RectTransform>();

            if(grid)
            {
                Debug.Log(rect.rect.height + " > " + grid.cellSize.y);
                if(rect.rect.width > grid.cellSize.x)
                    Scaled(item, grid.cellSize.x/rect.rect.width);
                else if(rect.rect.height > grid.cellSize.y)
                    Scaled(item, grid.cellSize.y/rect.rect.height);
                else
                    Sticky(item);
            }
            else
                Normal(item);
        }

        public void Normal(QuestDragNDropItem item)
        {
            /*
            RectTransform rect = item.transform.GetChild(0).GetComponent<RectTransform>();
            RectTransform rectParent = item.transform.GetComponent<RectTransform>();

            rectParent.pivot = Vector2.one * 0.5f;
            rectParent.anchorMax = Vector2.one * 0.5f;
            rectParent.anchorMin = Vector2.one * 0.5f;

            rectParent.anchoredPosition = Vector2.zero;

            rect.pivot = Vector2.one * 0.5f;
            rect.anchorMax = Vector2.one * 0.5f;
            rect.anchorMin = Vector2.one * 0.5f;

            rect.anchoredPosition = Vector2.zero;
            */
        }

        public void Sticky(QuestDragNDropItem item)
        {
            RectTransform rect = item.transform.GetChild(0).GetComponent<RectTransform>();
            RectTransform rectParent = item.transform.GetComponent<RectTransform>();

            rectParent.pivot = Vector2.one * 0.5f;
            rectParent.anchorMax = Vector2.one * 0.5f;
            rectParent.anchorMin = Vector2.one * 0.5f;

            rectParent.anchoredPosition = Vector2.zero;

            rect.pivot = Vector2.one * 0.5f;
            rect.anchorMax = Vector2.one * 0.5f;
            rect.anchorMin = Vector2.one * 0.5f;

            rect.localScale = Vector3.one;

            rect.anchoredPosition = Vector2.zero;
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

            //rect.anchoredPosition = Vector2.zero;

            rect.localScale = Vector3.one * scale;
        }

        public void FollowParent(QuestDragNDropItem item)
        {
            RectTransform rect = item.transform.GetChild(0).GetComponent<RectTransform>();
            GridLayoutGroup grid = item.transform.GetComponentInParent<GridLayoutGroup>();

            rect.pivot = Vector2.one * 0.5f;
            rect.anchorMax = Vector2.one * 0.5f;
            rect.anchorMin = Vector2.one * 0.5f;

            rect.sizeDelta = grid.cellSize;
        }

        public void OnAccept()
        {
            if(animator)
                animator.Play("Accept");

            if(acceptSound)
                GameManager.Instance.AudioSource.PlayOneShot(acceptSound);
        }

        public void OnReject()
        {
            if(animator)
                animator.Play("Reject");

            if(rejectSound)
                GameManager.Instance.AudioSource.PlayOneShot(rejectSound);
        }
    }
}