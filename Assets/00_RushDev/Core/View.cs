using UnityEngine;

namespace Rush
{
    public abstract class View : Extenable
    {
        [SerializeField]
        protected GameObject m_Content;

        protected virtual bool IsShowInternal
        {
            get
            {
                return m_Content.activeSelf;
            }
        }
        protected virtual bool CanShowInternal
        {
            get
            {
                return !IsShowInternal;
            }
        }
        public void Show()
        {
            ShowInternal();
        }
        public void Hide()
        {
            HideInternal();
        }

        protected virtual void ShowInternal()
        {
            if (CanShowInternal)
            {
                m_Content.SetActive(true);
            }
        }
        protected virtual void HideInternal()
        {
            if (IsShowInternal)
            {
                m_Content.SetActive(false);
            }
        }
    }
}
