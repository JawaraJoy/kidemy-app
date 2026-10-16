using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public class FeedbackItemAudio : FeedbackItem
    {
        [SerializeField] [FormerlySerializedAs("audioClip")] private AudioClip m_AudioClip;
        [SerializeField] private AudioSource m_AudioSource;

        void Awake()
        {
            
        }

        protected override void ExecPlayInternal()
        {
            if(m_AudioClip && m_AudioSource)
                m_AudioSource.PlayOneShot(m_AudioClip);
        }
    }
}