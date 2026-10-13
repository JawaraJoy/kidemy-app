using UnityEngine;

namespace EduGame
{
    public class Feedback : MonoBehaviour
    {
        private FeedbackItem[] m_FeedbackItems;

        protected virtual void Start()
        {
            m_FeedbackItems = GetComponentsInChildren<FeedbackItem>();
        }

        public virtual void Play(Transform transform)
        {
            if (m_FeedbackItems == null) return;
            if (transform == null) return;
            foreach (var feedbackItem in m_FeedbackItems)
            {
                if (feedbackItem != null)
                {
                    feedbackItem.Play(transform);
                }
            }
                
        }
    }
}