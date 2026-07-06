using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class AudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip audioClip;

        private Button button;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            button = GetComponent<Button>();

            if(!button)
                Debug.LogError("Button Component not found");
        }

        public void SetAudioClip(AudioClip audioClip)
        {
            this.audioClip = audioClip;
        }

        public void OnClick()
        {
            if(audioClip && GameManager.Instance.AudioSource)
                GameManager.Instance.AudioSource.PlayOneShot(audioClip);
        }
    }
}