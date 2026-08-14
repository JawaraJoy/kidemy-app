using System;
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
        private int totalLimit = 0;

        protected override void Start()
        {
            base.Start();

            if (!dataDragNDrop)
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
                    if(dataDragNDrop.Slots[i].Limit > 0)
                    {
                        slots[i] = InstantiateSlot(slots[0]);
                        slots[i].SetSlot(dataDragNDrop.Slots[i], i+1, i == dataDragNDrop.Items.Length - 1);

                        totalLimit += slots[i].GroupData.Limit;
                    }
                }
            }

            SetDialog();
        }

        public override void Setup()
        {
            dataDragNDrop = data as SO_QuestDragNDrop;

            base.Setup();
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

            if (questDragNDropZone)
                questDragNDropZone.RegisterItems();
        }

        public override void Enabled()
        {
            base.Enabled();

            Invoke("RecalculateContainer", 0.5f);
        }

        public override void OnAnswered(bool answer, bool submit = true)
        {
            if (dataDragNDrop.AutoSubmit)
            {
                int answereds = 0;

                foreach (var slot in slots)
                {
                    if(slot)
                        answereds += slot.DropZone.transform.childCount;
                }

                if (answereds >= dataDragNDrop.Items.Length || answereds >= totalLimit)
                    Submit();
            }
        }

        public override void Submit(int star = 1)
        {
            int totalAnswers = dataDragNDrop.AnswerInOrder ? Sort() : Check();

            if (totalAnswers == dataDragNDrop.Items.Length || totalAnswers >= totalLimit)
                star = 3;
            else if (totalAnswers / dataDragNDrop.Items.Length >= 0.5f)
                star = 2;

            GameManager.Instance.Submit(star);
        }

        protected virtual int Sort()
        {
            Dictionary<int, int> answers = new Dictionary<int, int>();

            int totalAnswers = 0;

            foreach (var slot in slots)
            {
                if(slot)
                {
                    QuestDragNDropItem[] answereds = slot.GetComponentsInChildren<QuestDragNDropItem>();

                    if (dataDragNDrop.CompiledMap[slot.GroupData.Id].Length == answereds.Length)
                    {
                        for (int i = 0; i < dataDragNDrop.CompiledMap[slot.GroupData.Id].Length; i++)
                        {
                            if (dataDragNDrop.CompiledMap[slot.GroupData.Id][i] == answereds[i].ItemData.Id)
                                totalAnswers++;
                        }
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
                if (slot && slot.DropZone)
                {
                    foreach (var item in slot.DropZone.Items)
                    {
                        if (dataDragNDrop.Index[item.ItemData.Id] == slot.GroupData.Id)
                            totalAnswers++;
                    }
                }
            }

            return totalAnswers;
        }

        public override void Reset()
        {
            if(items != null)
            {
                foreach (var choice in items)
                    choice.transform.SetParent(itemsContainer);
            }
                
            if(items != null)
            {
                foreach (var slot in slots)
                    if(slot)
                        slot.DropZone.RegisterItems();
            }
            
            SetDialog();

            base.Reset();
        }

        void SetDialog()
        {
            if (!string.IsNullOrEmpty(dataDragNDrop.Question.Text))
                GameManager.Instance.SetNPCDialog(dataDragNDrop.Question.Text);
        }


        /// <summary>
        /// Returns all VoiceRequests needed by this quest.
        /// Reads question text from 'data' and voice_id from 'character'.
        /// </summary>
        public override VoiceRequest[] GetVoiceRequests()
        {
            Setup();

            if (dataDragNDrop != null && dataDragNDrop.Groups.Length > 0)
            {
                List<VoiceRequest> voiceRequests = new List<VoiceRequest>();

                VoiceRequest[] baseVoiceRequest = base.GetVoiceRequests();

                if (baseVoiceRequest.Length > 0)
                    voiceRequests.Add(baseVoiceRequest[0]);

                for (int i = 0; i < dataDragNDrop.Groups.Length; i++)
                {
                    for (int j = 0; j < dataDragNDrop.Groups[i].Labels.Length; j++)
                    {
                        string labelText = dataDragNDrop.Groups[i].Labels[j].Text;

                        if (!string.IsNullOrEmpty(labelText))
                        {
                            voiceRequests.Add(new VoiceRequest
                            {
                                id = name + "_group_" + i + "_label_" + j,
                                text = labelText,
                                voice_id = GetVoiceId()
                            });
                        }
                    }
                }

                return voiceRequests.ToArray();
            }

            return Array.Empty<VoiceRequest>();
        }

        /// <summary>
        /// Checks if a specific voice request already has an AudioClip assigned in 'data'.
        /// </summary>
        public override bool HasVoiceClip(string requestId)
        {
            if (requestId.IndexOf("_group_") > 0)
            {
                int[] index = StringHelper.ExtractItemAndLabelIds(requestId);

                if (index != null && index.Length == 2)
                    return dataDragNDrop.Groups[index[0]] != null && dataDragNDrop.Groups[index[0]].Labels[index[1]] != null && dataDragNDrop.Groups[index[0]].Labels[index[1]].Audio != null;
            }
            else if (requestId.IndexOf("_question") > 0)
                return dataDragNDrop.Question.Audio != null;

            return false;
        }

        /// <summary>
        /// Assigns the downloaded & imported AudioClip directly to the SO_Quest referenced in 'data'.
        /// </summary>
        public override void AssignVoiceClip(string requestId, AudioClip clip)
        {
            if (requestId.IndexOf("_group_") > 0)
            {
                int[] index = StringHelper.ExtractItemAndLabelIds(requestId);

                if (index != null && index.Length == 2)
                    dataDragNDrop.Groups[index[0]].Labels[index[1]].SetAudio(clip);
            }
            else if (requestId.IndexOf("_question") > 0)
                data.Question.SetAudio(clip);
        }
    }
}