using System;
using UnityEngine;

namespace EduGame
{
    [Serializable]
    public class QuestUtilGameObject : MonoBehaviour
    {
        private RectTransform m_Rect;
        private int m_Id;

        public RectTransform Rect => m_Rect;
        public int Id => m_Id;

        protected virtual void Awake()
        {
            TryGetComponent<RectTransform>(out m_Rect);
        }

        public void SetId(int value)
        {
            m_Id = value;
        }

        protected virtual void Enable()
        {
            gameObject.SetActive(true);
        }

        public virtual void Disable()
        {
            gameObject.SetActive(false);
        }

        public virtual void Reset()
        {
            Enable();
        }
    }
}
