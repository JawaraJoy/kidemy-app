using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestDragNDrop : QuestZonePairing
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
        private int unansweredQuestion = 0;

        protected override void Start()
        {
            base.Start();

            dataDragNDrop = data as SO_QuestDragNDrop;

            if(!dataDragNDrop)
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

            SetDialog();
        }

        QuestDragNDropItem InstantiateItem(QuestDragNDropItem prefab)
        {
            QuestDragNDropItem item = Instantiate(prefab ? prefab : itemPrefab, itemsContainer);

            item.transform.localPosition = Vector3.zero;
            item.Rect.localScale = Vector3.one;

            return item;
        }

        QuestDragNDropSlot InstantiateSlot(QuestDragNDropSlot prefab)
        {
            QuestDragNDropSlot slot = Instantiate(prefab ? prefab : slotPrefab, slotsContainer);

            slot.transform.localPosition = Vector3.zero;
            slot.Rect.localScale = Vector3.one;

            return slot;
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

        public override void OnAnswered(bool answer, bool submit = true)
        {
            if(dataDragNDrop.AutoSubmit)
            {   
                int answereds = 0;

                foreach (var slot in slots)
                    answereds += slot.DropZone.transform.childCount;
                
                if(answereds >= dataDragNDrop.Items.Length)
                    Submit();
            }
        }

        public override void Submit(int star = 1)
        {
            int totalAnswers = dataDragNDrop.AnswerInOrder ? Sort() : Check();

            if(totalAnswers == dataDragNDrop.Items.Length)
                star = 3;
            else if(totalAnswers/dataDragNDrop.Items.Length >= 0.5f)
                star = 2;

            GameManager.Instance.Submit(star);
        }

        protected virtual int Sort()
        {
            Dictionary<int, int> answers = new Dictionary<int, int>();

            int totalAnswers = 0;

            foreach (var slot in slots)
            {
                QuestDragNDropItem[] answereds = slot.GetComponentsInChildren<QuestDragNDropItem>();
                
                if(dataDragNDrop.CompiledMap[slot.GroupData.Id].Length == answereds.Length)
                {
                    for(int i = 0; i < dataDragNDrop.CompiledMap[slot.GroupData.Id].Length; i++)
                    {
                        if(dataDragNDrop.CompiledMap[slot.GroupData.Id][i] == answereds[i].ItemData.Id)
                            totalAnswers++;
                    }
                }
            }

            return totalAnswers;
        }

        protected virtual int Check()
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

            return totalAnswers;
        }

        public override void Reset()
        {
            foreach (var choice in items)
                choice.transform.SetParent(itemsContainer);
        
            SetDialog();
        }

        void SetDialog()
        {
            if (!string.IsNullOrEmpty(dataDragNDrop.Question.Text))
                GameManager.Instance.SetNPCDialog(dataDragNDrop.Question.Text);
        }
    }
}