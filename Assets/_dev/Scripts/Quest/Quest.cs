using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public abstract class Quest : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] protected Button restartButton;
        [SerializeField] protected Button nextButton;
        [SerializeField] protected Button submitButton;

        [Header("Rules")]
        [SerializeField] protected int score = 10;

        protected SO_Quest data;
        protected bool isAnswered = false;
        protected bool isCorrect = false;
        
        protected virtual void Start()
        {
            if (restartButton)
                restartButton.gameObject.SetActive(false);
            
            if (nextButton)
                nextButton.gameObject.SetActive(false);
        }

        public virtual void Init(SO_Quest data)
        {
            this.data = data;

            ChallengeManager.Instance.InitQuest(data);
        }

        public virtual void OnAnswered(bool result, bool submit = true)
        {
            isAnswered = true;

            if(submit)
                Submit(result);
        }

        public virtual void Submit(bool result)
        {
            ChallengeManager.Instance.Submit(result, score);
        }

        public virtual void Next()
        {
            ChallengeManager.Instance.Next();
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