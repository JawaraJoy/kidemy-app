using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public abstract class FeedbackItem : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("delay")] protected float m_Delay = 0f;
        [SerializeField] [FormerlySerializedAs("delayStop")] protected float m_DelayStop = 0f;

        public virtual void Play(Transform transform)
        {
            if(m_Delay > 0)
                Invoke("ExecPlay", m_Delay);
            else
                ExecPlayInternal();
        }

        public virtual void Stop(Transform transform)
        {
            if(m_Delay > 0)
                Invoke("ExecStop", m_Delay);
            else
                ExecStopInternal();
        }

        public void ExecPlay()
        {
            ExecPlayInternal();
        }

        protected virtual void ExecPlayInternal()
        {
            
        }

        public void ExecStop()
        {
            ExecStopInternal();
        }

        protected virtual void ExecStopInternal()
        {
            
        }
    }
}