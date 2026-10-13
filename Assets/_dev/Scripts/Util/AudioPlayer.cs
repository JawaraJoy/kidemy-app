using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class AudioPlayer : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("audioClip")] private AudioClip m_AudioClip;

        private Button m_Button;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            TryGetComponent<Button>(out m_Button);

            if(!m_Button)
                Debug.LogError("Button Component not found");
        }

        public void SetAudioClip(AudioClip audioClip)
        {
            this.m_AudioClip = audioClip;
        }

        public void OnClick()
        {
            if(m_AudioClip && GameManager.Instance.AudioSource)
                GameManager.Instance.AudioSource.PlayOneShot(m_AudioClip);
        }
    }
}