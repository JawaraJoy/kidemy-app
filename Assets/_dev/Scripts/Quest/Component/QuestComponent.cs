using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public abstract class QuestComponent : MonoBehaviour
    {
        private RectTransform m_CachedRect;
        private Button m_CachedButton;
        private Image m_CachedImage;

        public RectTransform Rect => m_CachedRect;
        public Button Button => m_CachedButton;
        public Image Image => m_CachedImage;

        protected Quest m_Quest;

        protected bool m_IsInit;

        protected virtual void Awake()
        {
            InitInternal();
        }

        public void Init()
        {
            InitInternal();
        }

        protected virtual void InitInternal()
        {
            if(!m_IsInit)
            {
                m_CachedRect = GetComponent<RectTransform>();
                m_CachedButton = GetComponentInChildren<Button>();
                m_CachedImage = GetComponent<Image>();

                m_IsInit = true;   
            }
        }

        protected virtual void Start()
        {
            m_Quest = GetComponentInParent<Quest>();
        }

        public virtual void OnClick()
        {
            
        }

        public void Disable()
        {
            DisableInternal();
        }

        protected virtual void DisableInternal()
        {
            
        }

        public void Reset()
        {
            ResetInternal();
        }

        protected virtual void ResetInternal()
        {
            
        }
    }
}