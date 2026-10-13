using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public abstract class QuestComponent : MonoBehaviour
    {
        public RectTransform Rect { get; private set; }
        public Button Button { get; private set; }
        public Image Image { get; private set; }

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
                Rect = GetComponent<RectTransform>();
                Button = GetComponentInChildren<Button>();
                Image = GetComponent<Image>();

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