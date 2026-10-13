using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public class FeedbackItemAudio : FeedbackItem
    {
        [SerializeField] [FormerlySerializedAs("audioClip")] private AudioClip m_AudioClip;

        void Awake()
        {
            
        }

        protected override void ExecPlayInternal()
        {
            Debug.Log("oooowa");

            Debug.Log(GameManager.Instance.AudioSource);

            if(m_AudioClip && GameManager.Instance.AudioSource)
                GameManager.Instance.AudioSource.PlayOneShot(m_AudioClip);
        }
    }
}