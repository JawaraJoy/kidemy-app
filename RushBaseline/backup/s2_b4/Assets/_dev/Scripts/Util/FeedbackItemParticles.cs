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

        public override void ExecPlay()
        {
            if(m_Particles)
                m_Particles.Play();
        }
    }
}