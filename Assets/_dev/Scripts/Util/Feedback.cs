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
            foreach (var feedbackItem in feedbackItems)
                feedbackItem.Play(transform);
        }
    }
}