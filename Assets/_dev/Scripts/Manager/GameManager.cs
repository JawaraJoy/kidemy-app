using UnityEngine;
using UnityEngine.SceneManagement;

namespace EduGame
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource audioSourceSFX;

        public static GameManager Instance { get; private set; }
        
        public AudioSource AudioSource => audioSource;
        public AudioSource AudioSourceSFX => audioSourceSFX;
        
        void Awake()
        {
            if(!audioSource)
                Debug.LogError("Audio Source is not set, no audio will be played");

            if(!Instance)
                Instance = this;
        }

        public void Restart()
        {
            Instance = null;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}