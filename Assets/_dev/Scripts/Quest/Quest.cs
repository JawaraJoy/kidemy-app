using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public abstract class Quest : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] protected TMP_Text title;
        [SerializeField] protected Button restartButton;
        [SerializeField] protected Button submitButton;
        
        protected virtual void Start()
        {
            if (title)
                title.text = "Question 1";
            
            if (restartButton)
                restartButton.gameObject.SetActive(false);
        }

        protected virtual void Init()
        {
            
        }

        protected virtual void Update()
        {
            
        }

        public virtual void OnAnswered(bool result)
        {
            
        }

        public virtual void Reset()
        {
            
        }
    }
}