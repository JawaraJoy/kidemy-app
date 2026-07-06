using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent
using UnityEngine.EventSystems; // Required for Pointer Interfaces

namespace EduGame
{
    public class ButtonEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Inspector Events")]
        [SerializeField] private UnityEvent<Transform> onPointerOver = new UnityEvent<Transform>();
        [SerializeField] private UnityEvent<Transform> onPointerExit = new UnityEvent<Transform>();
        [SerializeField] private UnityEvent<Transform> onPointerClick = new UnityEvent<Transform>();

        // Called when the mouse starts hovering over the object
        public void OnPointerEnter(PointerEventData eventData)
        {
            onPointerOver?.Invoke(transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onPointerExit?.Invoke(transform);
        }

        // Called when the object is clicked
        public void OnPointerClick(PointerEventData eventData)
        {
            onPointerClick?.Invoke(transform);
        }

        public void AddEventOnPointerOver(UnityAction<Transform> overAction)
        {
            if (overAction != null) 
                onPointerOver.AddListener(overAction);
        }

        public void AddEventOnPointerExit(UnityAction<Transform> overAction)
        {
            if (overAction != null) 
                onPointerExit.AddListener(overAction);
        }

        public void AddEventOnPointerClick(UnityAction<Transform> clickAction)
        {
            if (clickAction != null) 
                onPointerClick.AddListener(clickAction);
        }
    }
}