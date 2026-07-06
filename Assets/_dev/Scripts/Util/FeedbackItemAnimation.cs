using System.Collections.Generic;
using UnityEngine;

namespace EduGame
{
    public class FeedbackItemAnimation : FeedbackItem
    {
        private Animator previousAnimator;
        private Animator selectedAnimator;
        private static Dictionary<int, Animator> cachedAnimator = new Dictionary<int, Animator>();
        
        [SerializeField] private string animationName;

        public override void Play(Transform transform)
        {
            if(selectedAnimator)
                previousAnimator = selectedAnimator;

            selectedAnimator = GetAnimator(transform);

            base.Play(transform);
        }

        public override void ExecPlay()
        {
            if(selectedAnimator)
            {
                if(previousAnimator)
                    previousAnimator.Play("Idle");

                selectedAnimator.enabled = true;        
                selectedAnimator.Play(animationName);
            }
        }

        private Animator GetAnimator(Transform transform)
        {
            Animator res = null;

            int id = transform.GetInstanceID();

            if(!cachedAnimator.ContainsKey(id))
            {
                Animator tAnimator = transform.GetComponent<Animator>();

                if(tAnimator)
                {
                    cachedAnimator.Add(id, tAnimator);
                    res = cachedAnimator[id];
                }
            }
            else
                res = cachedAnimator[id];

            return res;
        }
    }
}