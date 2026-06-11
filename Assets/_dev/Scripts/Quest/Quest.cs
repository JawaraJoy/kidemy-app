using UnityEngine;

namespace EduGame
{
    public abstract class Quest : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] protected SO_Quest data;

        [Header("Theme")]
        [SerializeField] protected Sprite background;
        [SerializeField] protected Color color;
        [SerializeField] protected RuntimeAnimatorController npcController;
        
        [Header("Rules")]
        [SerializeField] protected int score = 10;

        protected bool isAnswered = false;
        protected bool isCorrect = false;

        public Sprite Background => background;
        public Color Color => color;
        public RuntimeAnimatorController NPCController => npcController;
        
        public SO_Quest Data => data;
        
        protected virtual void Start()
        {
            if(!data)
                Debug.LogError("Quest not set");
            
            if(npcController)
                GameManager.Instance.SetNPC(npcController);

            GameManager.Instance.InitQuest(data);
        }

        public virtual void OnAnswered(bool result, bool submit = true)
        {
            isAnswered = true;

            if(submit)
                Submit(result ? 3 : 1);
        }

        public virtual void Submit(int star = 1)
        {
            GameManager.Instance.Submit(star);
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