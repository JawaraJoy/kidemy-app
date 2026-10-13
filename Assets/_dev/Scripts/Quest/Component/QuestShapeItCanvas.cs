using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestShapeItCanvas : QuestComponent
    {
        protected QuestShapeItItem[] m_Items;
        protected QuestDragNDropZone[] m_Zones;

        public QuestShapeItItem[] Items => m_Items;
        public QuestDragNDropZone[] Zones => m_Zones;
        
        protected override void Awake()
        {
            base.Awake();

            m_Items = GetComponentsInChildren<QuestShapeItItem>();
            m_Zones = GetComponentsInChildren<QuestDragNDropZone>();

            if(!Rect)
                Debug.LogError("Gameobject is not UI");

            Reset();
        }
    }
}