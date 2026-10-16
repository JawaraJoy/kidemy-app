using UnityEngine;
using UnityEngine.Events;

namespace EduGame
{
    [System.Serializable]
    public class PanelView
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

        private void ShowInternal()
        {
            m_Content.SetActive(true);
            m_OnShow.Invoke();
        } 
        private void HideInternal()
        {
            m_Content.SetActive(false);
            m_OnHide.Invoke();
        }
    }
}
