using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestShapeItCanvas : QuestComponent
    {
        protected QuestDragNDropItem[] items;
        protected QuestDragNDropZone[] zones;

        public QuestDragNDropItem[] Items => items;
        public QuestDragNDropZone[] Zones => zones;
        protected override void Awake()
        {
            base.Awake();

            items = GetComponentsInChildren<QuestDragNDropItem>();
            zones = GetComponentsInChildren<QuestDragNDropZone>();

            if(!Rect)
                Debug.LogError("Gameobject is not UI");

            Reset();
        }

        public override void OnClick()
        {
            
        }
    }
}