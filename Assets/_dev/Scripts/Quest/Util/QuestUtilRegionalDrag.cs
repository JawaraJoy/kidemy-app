using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestUtilRegionalDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Range(0, 1)]
        //[SerializeField] private float alphaThreshold = 0.1f;

        [SerializeField] private UnityEvent onclickEvent = new UnityEvent();


        private RawImage image; // Drag your Image GameObject here in the Inspector
        private RectTransform rectTransform;
        private Canvas parentCanvas;

        public bool Dragged { get; private set; }
        public PointerEventData PointerData { get; private set; }

        void Start()
        {
            image = GetComponent<RawImage>();

            if (!image)
                Debug.LogError("Image Component not found!");

            rectTransform = GetComponent<RectTransform>();

            if (!rectTransform)
                Debug.LogError("RectTransform Component not found!");

            // Find the canvas this object belongs to
            parentCanvas = GetComponentInParent<Canvas>();

            if (!parentCanvas)
                Debug.LogError("Parent not found!");
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            Dragged = true;
            PointerData = eventData;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            Dragged = true;
            PointerData = eventData;
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            Dragged = false;
        }
    }
}