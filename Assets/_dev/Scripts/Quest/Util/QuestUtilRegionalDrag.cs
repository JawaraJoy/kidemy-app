using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestUtilRegionalDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Range(0, 1)]
        //[SerializeField] private float alphaThreshold = 0.1f;

        [SerializeField] [FormerlySerializedAs("onclickEvent")] private UnityEvent m_OnclickEvent = new UnityEvent();


        private RawImage m_Image; // Drag your Image GameObject here in the Inspector
        private RectTransform m_RectTransform;
        private Canvas m_ParentCanvas;

        private bool m_Dragged;
        private PointerEventData m_PointerData;

        public bool Dragged => m_Dragged;
        public PointerEventData PointerData => m_PointerData;

        void Start()
        {
            m_Image = GetComponent<RawImage>();

            if (!m_Image)
                Debug.LogError("Image Component not found!");

            m_RectTransform = GetComponent<RectTransform>();

            if (!m_RectTransform)
                Debug.LogError("RectTransform Component not found!");

            // Find the canvas this object belongs to
            m_ParentCanvas = GetComponentInParent<Canvas>();

            if (!m_ParentCanvas)
                Debug.LogError("Parent not found!");
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            m_Dragged = true;
            m_PointerData = eventData;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            m_Dragged = true;
            m_PointerData = eventData;
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            m_Dragged = false;
        }
    }
}