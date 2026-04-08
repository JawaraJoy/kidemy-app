using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestUtilRegionalClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private UnityEvent onclickEvent = new UnityEvent();

        private Image image; // Drag your Image GameObject here in the Inspector

        void Start()
        {
            image = GetComponent<Image>();

            // Set the threshold. Pixels with alpha less than 0.5 will be ignored by clicks.
            if (image != null)
                image.alphaHitTestMinimumThreshold = 0.5f;
        }

        public void AddClickEvent(UnityAction unityAction)
        {
            onclickEvent.AddListener(unityAction);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Get the RectTransform of the clicked element
            RectTransform clickedRectTransform = GetComponent<RectTransform>();

            // Convert the screen point to a local point within the RectTransform's rectangle
            Vector2 localCursor;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                clickedRectTransform,
                eventData.position,
                eventData.pressEventCamera, // Automatically handles different Canvas Render Modes
                out localCursor))
            {
                // 'localCursor' now contains the coordinates relative to the center of the UI element's RectTransform pivot.
                // The bottom-left of the screen is (0, 0) in Input.mousePosition, but the local coordinates 
                // depend on the pivot and size of the RectTransform.

                Debug.Log("Clicked Canvas Element: " + gameObject.name);
                Debug.Log("Click coordinates relative to UI element: " + localCursor);

                // If you need coordinates relative to the bottom-left corner of the element,
                // you might need additional calculations involving the pivot. The 'localCursor' 
                // is usually sufficient for most use cases like placing markers or detecting positions on a map.
            }

            onclickEvent.Invoke();
        }
    }
}