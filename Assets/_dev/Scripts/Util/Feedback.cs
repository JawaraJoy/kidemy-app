using UnityEngine;
using MoreMountains.Feedbacks;

namespace EduGame
{
    public class Feedback : MonoBehaviour
    {
        private const float c_JuiceIntensity = 1f;

        [SerializeField] private MMF_Player m_Juice;

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

            if (m_Juice)
                m_Juice.PlayFeedbacks(transform.position, c_JuiceIntensity, false);
                
        }
    }
}