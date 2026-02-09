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

        protected virtual void Awake()
        {
            Rect = GetComponent<RectTransform>();
            Button = GetComponent<Button>();
            Image = GetComponent<Image>();
        }

        public virtual void Init()
        {
            
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