using UnityEngine;
using UnityEngine.SceneManagement;

namespace EduGame
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

        public static GameManager Instance { get; private set; }
        public AudioSource AudioSource => audioSource;
        
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