using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestDragNDrop", menuName = "EduGame/Quest/DragNDrop")]
    public class SO_QuestDragNDrop : SO_Quest
    {
        [Header("Question")]
        [SerializeField] [FormerlySerializedAs("groups")] private QuestUtilLabelGroup[] m_Groups;

        [Header("Rule")]
        [SerializeField] [FormerlySerializedAs("randomizeChoice")] private bool m_RandomizeChoice;
        [SerializeField] [FormerlySerializedAs("answerInOrder")] private bool m_AnswerInOrder;
        [SerializeField] [FormerlySerializedAs("timer")] private float m_Timer = 0;
        
        public QuestUtilLabel[] Items => GetItems();
        public QuestUtilLabelGroup[] Groups => m_Groups;
        public Dictionary<int, int> Index => m_CompiledIndex;
        public Dictionary<int, int[]> CompiledMap => m_CompiledMap;
        public QuestUtilLabelGroup[] Slots => m_Slots;
        public bool AnswerInOrder => m_AnswerInOrder;
        public float Timer => m_Timer;

        [NonSerialized] private Dictionary<int, int> m_CompiledIndex = new Dictionary<int, int>();
        [NonSerialized] private Dictionary<int, int[]> m_CompiledMap = new Dictionary<int, int[]>();
        [NonSerialized] private QuestUtilLabel[] m_CompiledItems;
        [NonSerialized] private QuestUtilLabelGroup[] m_Slots;

        QuestUtilLabel[] GetItems()
        {
            if(m_CompiledItems == null)
            {
                int totalItem = 0;
                List<QuestUtilLabel> items = new List<QuestUtilLabel>();

                m_Slots = new QuestUtilLabelGroup[m_Groups.Length];

                int groupIndex = 0;

                foreach (var group in m_Groups)
                {
                    group.Id = groupIndex;
                    m_Slots[group.Id] = group;
                    
                    if(!m_CompiledMap.ContainsKey(group.Id))
                        m_CompiledMap.Add(group.Id, new int[group.Labels.Length]);

                    int itemIndex = 0;

                    foreach (var item in group.Labels)
                    {
                        item.Id = totalItem + itemIndex;
                    
                        items.Add(item);
                        m_CompiledIndex.Add(item.Id, group.Id);
                        m_CompiledMap[group.Id][itemIndex] = item.Id;

                        itemIndex++;
                    }

                    totalItem += group.Labels.Length;

                    groupIndex++;
                }

                m_CompiledItems = new QuestUtilLabel[totalItem]; 
                
                if(m_RandomizeChoice)
                {
                    System.Random random = new System.Random();
                    m_CompiledItems = items.OrderBy(x => random.Next()).ToArray();
                }
                else
                    m_CompiledItems = items.ToArray();
            }

            return m_CompiledItems;
        }
    }
}