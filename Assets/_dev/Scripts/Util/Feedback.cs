using UnityEngine;

namespace EduGame
{
    public class Feedback : MonoBehaviour
    {
        private FeedbackItem[] feedbackItems;

        protected virtual void Start()
        {
            feedbackItems = GetComponentsInChildren<FeedbackItem>();
        }

        public virtual void Play(Transform transform)
        {
            if (feedbackItems == null) return;
            if (transform == null) return;
            foreach (var feedbackItem in feedbackItems)
            {
                if (feedbackItem != null)
                {
                    feedbackItem.Play(transform);
                }
            }
                
        }
    }
}