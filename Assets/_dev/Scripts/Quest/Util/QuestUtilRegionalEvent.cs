using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestUtilRegionalEvent : MonoBehaviour, IPointerClickHandler
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
            onclickEvent.Invoke();
        }
    }
}