using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public abstract class Quest : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] protected SO_Quest data;
        
        [Header("Rules")]
        [SerializeField] protected int score = 10;

        protected bool isAnswered = false;
        protected bool isCorrect = false;
        
        protected virtual void Start()
        {
            if(!data)
                Debug.LogError("Quest not set");

            GameManager.Instance.InitQuest(data);
        }

        public virtual void OnAnswered(bool result, bool submit = true)
        {
            isAnswered = true;

            if(submit)
                Submit(result);
        }

        public virtual void Submit(bool result)
        {
            GameManager.Instance.Submit(result, score);
        }

        public virtual void Next()
        {
            GameManager.Instance.Next();
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