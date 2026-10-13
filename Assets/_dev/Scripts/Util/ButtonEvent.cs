using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Events; // Required for UnityEvent
using UnityEngine.EventSystems; // Required for Pointer Interfaces

namespace EduGame
{
    public class ButtonEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Inspector Events")]
        [SerializeField] [FormerlySerializedAs("onPointerOver")] private UnityEvent<Transform> m_OnPointerOver = new UnityEvent<Transform>();
        [SerializeField] [FormerlySerializedAs("onPointerExit")] private UnityEvent<Transform> m_OnPointerExit = new UnityEvent<Transform>();
        [SerializeField] [FormerlySerializedAs("onPointerClick")] private UnityEvent<Transform> m_OnPointerClick = new UnityEvent<Transform>();

        // Called when the mouse starts hovering over the object
        public void OnPointerEnter(PointerEventData eventData)
        {
            m_OnPointerOver?.Invoke(transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_OnPointerExit?.Invoke(transform);
        }

        // Called when the object is clicked
        public void OnPointerClick(PointerEventData eventData)
        {
            m_OnPointerClick?.Invoke(transform);
        }

        public void AddEventOnPointerOver(UnityAction<Transform> overAction)
        {
            if (overAction != null) 
                m_OnPointerOver.AddListener(overAction);
        }

        public void AddEventOnPointerExit(UnityAction<Transform> overAction)
        {
            if (overAction != null) 
                m_OnPointerExit.AddListener(overAction);
        }

        public void AddEventOnPointerClick(UnityAction<Transform> clickAction)
        {
            if (clickAction != null) 
                m_OnPointerClick.AddListener(clickAction);
        }
    }
}