using UnityEngine;

namespace EduGame
{
    public class FeedbackItemParticle : FeedbackItem
    {
        [SerializeField] private ParticleSystem particles;

        void Awake()
        {
            if(particles)
            {
                ParticleSystem.MainModule particleSettings = particles.main;
                particleSettings.playOnAwake = false;
            }
        }

        public override void ExecPlay()
        {
            if(particles)
                particles.Play();
        }
    }
}