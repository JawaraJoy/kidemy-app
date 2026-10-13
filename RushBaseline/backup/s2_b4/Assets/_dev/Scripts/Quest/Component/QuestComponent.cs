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
            Init();
        }

        public virtual void Init()
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

        public virtual void Disable()
        {
            
        }

        public virtual void Reset()
        {
            
        }
    }
}