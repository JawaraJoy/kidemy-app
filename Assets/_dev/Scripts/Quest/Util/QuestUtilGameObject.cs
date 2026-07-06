using System;
using UnityEngine;

namespace EduGame
{
    [Serializable]
    public class QuestUtilGameObject : MonoBehaviour
    {
        public RectTransform Rect { get; private set; }
        public int Id { get; set; }

        protected virtual void Awake()
        {
            Rect = GetComponent<RectTransform>();
        }

        public virtual void Enable()
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
