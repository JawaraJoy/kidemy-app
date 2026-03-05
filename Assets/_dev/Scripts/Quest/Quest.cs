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
        [SerializeField] protected Button nextButton;

        protected SO_Quest data;
        protected ChallengeManager challenge;
        protected bool isAnswered = false;
        protected bool isCorrect = false;
        
        protected virtual void Start()
        {
            if (restartButton)
                restartButton.gameObject.SetActive(false);
            
            if (nextButton)
                nextButton.gameObject.SetActive(false);

            challenge = GetComponentInParent<ChallengeManager>();

            if(!challenge)
                Debug.LogError("ChallengeManager componenet not found in parent, make sure it setup properly");

            if (title)
                title.text = "Question " + challenge.CurrentQuestNumber;
        }

        public virtual void Init(SO_Quest data)
        {
            this.data = data;
        }

        public virtual void OnAnswered(bool result)
        {
            isAnswered = true;
            
            if (restartButton)
                restartButton.gameObject.SetActive(true);
            
            if (nextButton)
                nextButton.gameObject.SetActive(true);

            if(!isCorrect && result)
            {
                isCorrect = result;    
                challenge.AddScore(data.Score);
            }

            if(data.AutoSubmit)
                Next();
        }

        public virtual void Next()
        {
            challenge.Next();
        }

        public virtual void Reset()
        {
            
        }

        public virtual void Disabled()
        {
            gameObject.SetActive(false);
        }

        public virtual void Enabled()
        {
            gameObject.SetActive(true);
        }
    }
}