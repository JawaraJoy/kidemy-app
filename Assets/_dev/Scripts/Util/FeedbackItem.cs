using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public abstract class FeedbackItem : MonoBehaviour
    {
        private const string c_PlayMethod = "ExecPlay";

        [SerializeField] [FormerlySerializedAs("delay")] protected float m_Delay = 0f;

        public virtual void Play(Transform transform)
        {
            if(m_Delay > 0)
                Invoke(c_PlayMethod, m_Delay);
            else
                ExecPlayInternal();
        }

        public void ExecPlay()
        {
            ExecPlayInternal();
        }

        protected virtual void ExecPlayInternal()
        {
            
        }
    }
}