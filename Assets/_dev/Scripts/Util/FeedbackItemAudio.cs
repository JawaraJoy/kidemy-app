using UnityEngine;

namespace EduGame
{
    public class FeedbackItemAudio : FeedbackItem
    {
        [SerializeField] private AudioClip audioClip;

        void Awake()
        {
            
        }

        public override void ExecPlay()
        {
            if(audioClip && GameManager.Instance.AudioSource)
                GameManager.Instance.AudioSource.PlayOneShot(audioClip);
        }
    }
}