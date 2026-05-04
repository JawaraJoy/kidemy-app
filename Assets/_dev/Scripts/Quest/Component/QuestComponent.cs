using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public abstract class QuestComponent : MonoBehaviour
    {
        public RectTransform Rect { get; private set; }
        public Button Button { get; private set; }
        public Image Image { get; private set; }

        protected Quest quest;

        protected bool isInit;

        protected virtual void Awake()
        {
            Init();
        }

        public virtual void Init()
        {
            if(!isInit)
            {
                Rect = GetComponent<RectTransform>();
                Button = GetComponentInChildren<Button>();
                Image = GetComponent<Image>();

                isInit = true;   
            }
        }

        protected virtual void Start()
        {
            quest = GetComponentInParent<Quest>();
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