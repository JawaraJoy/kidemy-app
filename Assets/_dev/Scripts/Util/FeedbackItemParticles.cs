using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public class FeedbackItemParticle : FeedbackItem
    {
        [SerializeField] [FormerlySerializedAs("particles")] private ParticleSystem m_Particles;

        void Awake()
        {
            if(m_Particles)
            {
                ParticleSystem.MainModule particleSettings = m_Particles.main;
                particleSettings.playOnAwake = false;
            }
        }

        protected override void ExecPlayInternal()
        {
            if(m_Particles)
                m_Particles.Play();
        }
    }
}