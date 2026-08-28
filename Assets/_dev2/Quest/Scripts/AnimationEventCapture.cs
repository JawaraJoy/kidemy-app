using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace EduGame
{
    public class AnimationEventCapture : MonoBehaviour
    {
        private AnimatorClipConfig m_PlayedClipConfig;
        [SerializeField]
        private AudioSource m_Audio;
        public void PlaySound()
        {
            if (!m_Audio) return;
            if (m_PlayedClipConfig)
            {
                if (m_PlayedClipConfig.SoundSFX)
                {
                    m_Audio.PlayOneShot(m_PlayedClipConfig.SoundSFX);
                }
            }
        }

        public void SetPlayedConfig(AnimatorClipConfig config)
        {
            m_PlayedClipConfig = config;
        }
    }
}
