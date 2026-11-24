using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestDragNDropZone : MonoBehaviour, IDropHandler
    {
        private const float c_ItemScale = 0.75f;
        private const float c_ScaleUp = 2f;
        private const float c_ScaleDown = 0.5f;
        private const string c_AcceptState = "Accept";
        private const string c_RejectState = "Reject";

        public enum DisplayMode { Normal, Dynamic, Sticky, Fill, ScaledUp, ScaledDown, ScaledDown2, FollowParent, Hide }

        [SerializeField] [FormerlySerializedAs("displayMode")] private DisplayMode m_DisplayMode;
        [SerializeField] [FormerlySerializedAs("dropInfo")] private TMP_Text m_DropInfo;
        [SerializeField] [FormerlySerializedAs("verify")] private bool m_Verify;
        [SerializeField] [FormerlySerializedAs("answer")] private bool m_Answer;

        [Header("Feedback")]
        [SerializeField] [FormerlySerializedAs("acceptSound")] private AudioClip m_AcceptSound;
        [SerializeField] [FormerlySerializedAs("rejectSound")] private AudioClip m_RejectSound;

        private QuestDragNDropItem[] m_Items;
        private QuestDragNDropSlot m_Slot;
        private QuestZonePairing m_Quest;
        private GridLayoutGroup m_Grid;
        private Animator m_Animator;

        public QuestDragNDropItem[] Items => m_Items;
        public DisplayMode CurrentDisplayMode => m_DisplayMode;
        public QuestDragNDropSlot Slot => m_Slot;

        void Awake()
        {
            m_Slot = GetComponentInParent<QuestDragNDropSlot>();

            m_Quest = GetComponentInParent<QuestZonePairing>();

            TryGetComponent<Animator>(out m_Animator);
            m_Grid = GetComponentInChildren<GridLayoutGroup>();

            RegisterItemsInternal();
        }

        public void AddItem(QuestDragNDropItem item)
        {
            AddItemInternal(item);
        }

        private void AddItemInternal(QuestDragNDropItem item)
        {
            if (item)
            {
                int itemIndex = m_Items.Length;
                bool isSameZone = false;

                if (item.OriginalZone && item.OriginalZone.GetEntityId() == transform.GetEntityId())
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

                ApplyDisplayMode(item);

                RegisterItemsInternal();
            }
        }

        public void RegisterItems()
        {
            RegisterItemsInternal();
        }

        private void RegisterItemsInternal()
        {
            var items = new System.Collections.Generic.List<QuestDragNDropItem>(transform.childCount);
            foreach (Transform child in transform)
                if (child.TryGetComponent(out QuestDragNDropItem item)) items.Add(item);
            m_Items = items.ToArray();
            if (m_DropInfo) m_DropInfo.gameObject.SetActive(m_Items.Length == 0);
        }

        private int GetItemIndex(QuestDragNDropItem item)
        {
            int index = System.Array.IndexOf(m_Items, item);
            return index >= 0 ? index : m_Items.Length;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null)
            {
                eventData.pointerDrag.TryGetComponent<QuestDragNDropItem>(out QuestDragNDropItem draggedItem);

                if (draggedItem)
                {
                    bool slotLimitVerification = draggedItem.OriginalZone == this || !m_Slot || (m_Slot.GroupData != null && (m_Slot.GroupData.Limit < 0 || m_Items.Length < m_Slot.GroupData.Limit));

                    if (slotLimitVerification && (!m_Verify || (m_Quest && m_Quest.Verify(draggedItem, this))))
                    {
                        AddItemInternal(draggedItem);

                        if (m_Answer && m_Quest) m_Quest.OnAnswered(true);

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

            ApplyDisplayMode(item);
            RegisterItemsInternal();
        }

        private void ApplyDisplayMode(QuestDragNDropItem item)
        {
            item.SetContainerVisible(!m_Answer);
            if (item.transform.childCount == 0) return;
            if (m_DisplayMode == DisplayMode.Hide)
            {
                item.transform.GetChild(0).gameObject.SetActive(false);
                return;
            }
            switch (m_DisplayMode)
            {
                case DisplayMode.Fill: FillZone(item); break;
                case DisplayMode.Dynamic: Dynamic(item); break;
                case DisplayMode.Sticky: Sticky(item); break;
                case DisplayMode.ScaledUp: Scaled(item, c_ScaleUp); break;
                case DisplayMode.ScaledDown: Scaled(item, c_ScaleDown); break;
                case DisplayMode.ScaledDown2: Scaled(item, c_ItemScale); break;
                case DisplayMode.FollowParent: FollowParent(item); break;
            }
        }

        private void Dynamic(QuestDragNDropItem item)
        {
            item.transform.GetChild(0).TryGetComponent<RectTransform>(out RectTransform rect);

            if(m_Grid)
            {
                if(rect.rect.width > m_Grid.cellSize.x)
                    Scaled(item, m_Grid.cellSize.x/rect.rect.width);
                else if(rect.rect.height > m_Grid.cellSize.y)
                    Scaled(item, m_Grid.cellSize.y/rect.rect.height);
                else
                    Sticky(item);
            }
        }

        private void Sticky(QuestDragNDropItem item)
        {
            item.transform.GetChild(0).TryGetComponent<RectTransform>(out RectTransform rect);
            item.transform.TryGetComponent<RectTransform>(out RectTransform rectParent);

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

        private void FillZone(QuestDragNDropItem item)
        {
            item.Rect.anchorMax = Vector2.one;
            item.Rect.anchorMin = Vector2.zero;

            item.Rect.pivot = Vector2.one * 0.5f;

            item.Rect.offsetMin = Vector2.zero;
            item.Rect.offsetMax = Vector2.zero;
        }

        private void Scaled(QuestDragNDropItem item, float scale)
        {
            item.transform.GetChild(0).TryGetComponent<RectTransform>(out RectTransform rect);

            rect.pivot = Vector2.one * 0.5f;
            rect.anchorMax = Vector2.one * 0.5f;
            rect.anchorMin = Vector2.one * 0.5f;

            //rect.anchoredPosition = Vector2.zero;

            rect.localScale = Vector3.one * scale;
        }

        private void FollowParent(QuestDragNDropItem item)
        {
            item.transform.GetChild(0).TryGetComponent<RectTransform>(out RectTransform rect);
            GridLayoutGroup grid = item.transform.GetComponentInParent<GridLayoutGroup>();

            rect.pivot = Vector2.one * 0.5f;
            rect.anchorMax = Vector2.one * 0.5f;
            rect.anchorMin = Vector2.one * 0.5f;

            rect.sizeDelta = grid.cellSize;
        }

        private void OnAccept()
        {
            if(m_Animator)
                m_Animator.Play(c_AcceptState);

            if (m_AcceptSound && m_Quest && m_Quest.Host?.AudioSource)
                m_Quest.Host.AudioSource.PlayOneShot(m_AcceptSound);
        }

        private void OnReject()
        {
            if(m_Animator)
                m_Animator.Play(c_RejectState);

            if (m_RejectSound && m_Quest && m_Quest.Host?.AudioSource)
                m_Quest.Host.AudioSource.PlayOneShot(m_RejectSound);
        }
    }
}