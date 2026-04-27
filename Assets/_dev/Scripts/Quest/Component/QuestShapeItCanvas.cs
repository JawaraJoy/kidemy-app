using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestShapeItCanvas : QuestComponent
    {
        protected QuestShapeItItem[] items;
        protected QuestDragNDropZone[] zones;

        public QuestShapeItItem[] Items => items;
        public QuestDragNDropZone[] Zones => zones;
        
        protected override void Awake()
        {
            base.Awake();

            items = GetComponentsInChildren<QuestShapeItItem>();
            zones = GetComponentsInChildren<QuestDragNDropZone>();

            Debug.Log(items.Length);

            if(!Rect)
                Debug.LogError("Gameobject is not UI");

            Reset();
        }
    }
}