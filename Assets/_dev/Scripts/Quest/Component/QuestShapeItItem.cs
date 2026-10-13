using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestShapeItItem : QuestDragNDropItem, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
    {   
        [SerializeField] [FormerlySerializedAs("canBeRotated")] private bool m_CanBeRotated = true;

        public bool CanBeRotated => m_CanBeRotated;
    }
}