using UnityEngine;

namespace EduGame
{
    public abstract class FeedbackItem : MonoBehaviour
    {
        [SerializeField] protected float delay = 0f;
        [SerializeField] protected float delayStop = 0f;

        public virtual void Play(Transform transform)
        {
            if(delay > 0)
                Invoke("ExecPlay", delay);
            else
                ExecPlay();
        }

        public virtual void Stop(Transform transform)
        {
            if(delay > 0)
                Invoke("ExecStop", delay);
            else
                ExecStop();
        }

        public virtual void ExecPlay()
        {
            
        }

        public virtual void ExecStop()
        {
            
        }
    }
}