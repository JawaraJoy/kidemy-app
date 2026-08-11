using UnityEngine;
using UnityEngine.Events;

namespace EduGame
{
    public abstract class PanelView : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_Content;

        [SerializeField]
        private UnityEvent m_OnShow;
        [SerializeField]
        private UnityEvent m_OnHide;

        public  void Show()
        {
            ShowInternal();
        }
        public void Hide()
        {
            HideInternal();
        }

        protected virtual void ShowInternal()
        {
            m_Content.SetActive(true);
            m_OnShow.Invoke();
        } 
        protected virtual void HideInternal()
        {
            m_Content.SetActive(false);
            m_OnHide.Invoke();
        }
    }
}
