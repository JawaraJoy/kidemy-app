using System;
using System.Collections.Generic;
using EasyTextEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestDragNDropPattern : QuestDragNDrop
    {
        [Header("Components")]
        [SerializeField] protected RectTransform[] slotPatterns;

        protected override void Start()
        {
            slotsContainer = null;

            if (slotPatterns.Length > 0 && slotPrefab)
            {
                slots = new QuestDragNDropSlot[slotPatterns.Length];

                int i = 0;

                List<QuestUtilLabelGroup> groups = new List<QuestUtilLabelGroup>();
                
                foreach (var slot in dataDragNDrop.Groups)
                {
                    if(slot.Limit > 0) groups.Add(slot);
                }

                while (i < slotPatterns.Length)
                {
                    if(i < groups.Count)
                    {
                        slots[i] = InstantiateSlot(slots[0], slotPatterns[i]);
                        slots[i].SetSlot(groups[i], i+1, i == groups.Count - 1);

                        totalLimit += slots[i].GroupData.Limit;
                    }

                    i++;
                }
            }

            base.Start();
        }
    }
}