using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestDragNDrop", menuName = "EduGame/Quest/DragNDrop")]
    public class SO_QuestDragNDrop : SO_Quest
    {
        [Header("Question")]
        [SerializeField] private QuestUtilLabelGroup[] groups;

        [Header("Rule")]
        [SerializeField] private bool randomizeChoice;
        [SerializeField] private bool answerInOrder;
        [SerializeField] private float timer = 0;
        
        public QuestUtilLabel[] Items => GetItems();
        public QuestUtilLabelGroup[] Groups => groups;
        public Dictionary<int, int> Index => compiledIndex;
        public Dictionary<int, int[]> CompiledMap => compiledMap;
        public QuestUtilLabelGroup[] Slots => slots;
        public bool AnswerInOrder => answerInOrder;
        public float Timer => timer;

        [NonSerialized] private Dictionary<int, int> compiledIndex = new Dictionary<int, int>();
        [NonSerialized] private Dictionary<int, int[]> compiledMap = new Dictionary<int, int[]>();
        [NonSerialized] private QuestUtilLabel[] compiledItems;
        [NonSerialized] private QuestUtilLabelGroup[] slots;

        QuestUtilLabel[] GetItems()
        {
            if(compiledItems == null)
            {
                int totalItem = 0;
                List<QuestUtilLabel> items = new List<QuestUtilLabel>();

                slots = new QuestUtilLabelGroup[groups.Length];

                int groupIndex = 0;

                foreach (var group in groups)
                {
                    group.Id = groupIndex;
                    slots[group.Id] = group;
                    
                    if(!compiledMap.ContainsKey(group.Id))
                        compiledMap.Add(group.Id, new int[group.Labels.Length]);

                    int itemIndex = 0;

                    foreach (var item in group.Labels)
                    {
                        item.Id = totalItem + itemIndex;
                    
                        items.Add(item);
                        compiledIndex.Add(item.Id, group.Id);
                        compiledMap[group.Id][itemIndex] = item.Id;

                        itemIndex++;
                    }

                    totalItem += group.Labels.Length;

                    groupIndex++;
                }

                compiledItems = new QuestUtilLabel[totalItem]; 
                
                if(randomizeChoice)
                {
                    System.Random random = new System.Random();
                    compiledItems = items.OrderBy(x => random.Next()).ToArray();
                }
                else
                    compiledItems = items.ToArray();
            }

            return compiledItems;
        }
    }
}