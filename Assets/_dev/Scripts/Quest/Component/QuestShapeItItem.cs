using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace EduGame
{
    public class QuestShapeItItem : QuestDragNDropItem, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
    {   
        [SerializeField] private bool canBeRotated = true;

        public bool CanBeRotated => canBeRotated;
    }
}