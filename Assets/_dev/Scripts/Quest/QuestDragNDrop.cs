using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestDragNDrop : Quest
    {
        [Header("Data")]
        
        [Header("Components")]
        [SerializeField] private Image questionImage;
        [SerializeField] private AudioPlayer questionAudio;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private RectTransform itemsContainer;
        [SerializeField] private RectTransform slotsContainer;

        [Header("Prefab")]
        [SerializeField] private QuestDragNDropItem itemPrefab;
        [SerializeField] private QuestDragNDropSlot slotPrefab;

        private SO_QuestDragNDrop dataDragNDrop;
        private QuestDragNDropItem[] items;
        private QuestDragNDropSlot[] slots;

        public override void Init(SO_Quest data)
        {
            dataDragNDrop = data as SO_QuestDragNDrop;

            if(dataDragNDrop)
                base.Init(data);
            else
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestMultipleChoice");

            if (questionImage)
            {
                if (dataDragNDrop.Question.Image)
                    questionImage.sprite = dataDragNDrop.Question.Image;
                else
                {
                    questionImage.gameObject.SetActive(false);
                    questionImage.transform.parent.gameObject.SetActive(false);
                }
            }

            if (questionAudio)
            {
                if (dataDragNDrop.Question.Audio)
                    questionAudio.SetAudioClip(dataDragNDrop.Question.Audio);
                else
                {
                    questionAudio.gameObject.SetActive(false);
                    questionAudio.transform.parent.gameObject.SetActive(false);
                }
            }

            if (questionText)
            {
                if (!string.IsNullOrEmpty(dataDragNDrop.Question.Text))
                    questionText.text = dataDragNDrop.Question.Text;
                else
                {
                    questionText.gameObject.SetActive(false);
                    questionText.transform.parent.gameObject.SetActive(false);
                }
            }

            if (itemsContainer && itemPrefab)
            {
                items = new QuestDragNDropItem[dataDragNDrop.Items.Length];

                for (int i = 0; i < dataDragNDrop.Items.Length; i++)
                {
                    items[i] = InstantiateItem(items[0]);
                    items[i].SetItem(dataDragNDrop.Items[i]);
                }
            }

            if (slotsContainer && slotPrefab)
            {
                slots = new QuestDragNDropSlot[dataDragNDrop.Slots.Length];

                for (int i = 0; i < dataDragNDrop.Slots.Length; i++)
                {
                    slots[i] = InstantiateSlot(slots[0]);
                    slots[i].SetSlot(dataDragNDrop.Slots[i]);
                }
            }

            if(nextButton && dataDragNDrop.AutoSubmit)
                nextButton.gameObject.SetActive(false);
        }

        QuestDragNDropItem InstantiateItem(QuestDragNDropItem prefab)
        {
            QuestDragNDropItem item = Instantiate(prefab ? prefab : itemPrefab);

            item.transform.SetParent(itemsContainer);
            item.transform.localPosition = Vector3.zero;
            item.Rect.localScale = Vector3.one;

            return item;
        }

        QuestDragNDropSlot InstantiateSlot(QuestDragNDropSlot prefab)
        {
            QuestDragNDropSlot slot = Instantiate(prefab ? prefab : slotPrefab);

            slot.transform.SetParent(slotsContainer);
            slot.transform.localPosition = Vector3.zero;
            slot.Rect.localScale = Vector3.one;

            return slot;
        }

        public override void Reset()
        {
            foreach (var choice in items)
                choice.Reset();

            if (restartButton)
                restartButton.gameObject.SetActive(false);
        }

        void RecalculateContainer()
        {
            if (items[0].Rect.sizeDelta.x > 0 && items[0].Rect.sizeDelta.y > 0)
            {
                GridLayoutGroup gridLayoutGroup = itemsContainer.GetComponent<GridLayoutGroup>();
                if (gridLayoutGroup)
                    gridLayoutGroup.cellSize = items[0].Rect.sizeDelta;
            }

            if (slots[0].Rect.sizeDelta.x > 0 && slots[0].Rect.sizeDelta.y > 0)
            {
                GridLayoutGroup gridLayoutGroup = slotsContainer.GetComponent<GridLayoutGroup>();
                if (gridLayoutGroup)
                    gridLayoutGroup.cellSize = slots[0].Rect.sizeDelta;
                
                slotsContainer.sizeDelta = new Vector2(slotsContainer.sizeDelta.x * slots.Length, slotsContainer.sizeDelta.y);
            }

            QuestDragNDropZone questDragNDropZone = itemsContainer.GetComponent<QuestDragNDropZone>();
                
            if(questDragNDropZone)
                questDragNDropZone.RegisterItems();
        }

        public override void Enabled()
        {
            base.Enabled();

            Invoke("RecalculateContainer", 0.5f);
        }

        public override void Submit(bool result)
        {
            int totalAnswers = 0;

            foreach (var slot in slots)
            {
                if(slot.DropZone)
                {
                    foreach (var item in slot.DropZone.Items)
                    {
                        if(dataDragNDrop.Index[item.ItemData.Id] == slot.GroupData.Id)
                            totalAnswers++;
                    }
                }
            }

            ChallengeManager.Instance.ShowResult(totalAnswers == dataDragNDrop.Items.Length);
        }
    }
}