using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public class FeedbackItemAnimation : FeedbackItem
    {
        private Animator m_PreviousAnimator;
        private Animator m_SelectedAnimator;
        private static Dictionary<int, Animator> s_CachedAnimator = new Dictionary<int, Animator>();
        
        [SerializeField] [FormerlySerializedAs("animationName")] private string m_AnimationName;

        public override void Play(Transform transform)
        {
            if(m_SelectedAnimator)
                m_PreviousAnimator = m_SelectedAnimator;

            m_SelectedAnimator = GetAnimator(transform);

            base.Play(transform);
        }

        public override void ExecPlay()
        {
            if(m_SelectedAnimator)
            {
                if(m_PreviousAnimator)
                    m_PreviousAnimator.Play("Idle");

                m_SelectedAnimator.enabled = true;        
                m_SelectedAnimator.Play(m_AnimationName);
            }
        }

        private Animator GetAnimator(Transform transform)
        {
            Animator res = null;

            int id = transform.GetEntityId().GetHashCode();

            if(!s_CachedAnimator.ContainsKey(id))
            {
                Animator tAnimator = transform.GetComponent<Animator>();

                if(tAnimator)
                {
                    s_CachedAnimator.Add(id, tAnimator);
                    res = s_CachedAnimator[id];
                }
            }
            else
                res = s_CachedAnimator[id];

            return res;
        }
    }
}