using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMultipleChoicesItem : QuestComponent
    {
        [SerializeField] protected TMP_Text text;
        [SerializeField] private Image image;
        
        [SerializeField] protected GameObject correct;
        [SerializeField] protected GameObject wrong;
        
        public bool IsRightAnswer { get; private set; } = false;

        protected QuestUtilLabelChoice choice;

        protected override void Awake()
        {
            base.Awake();

            if(!Rect)
                Debug.LogError("Gameobject is not UI");

            if(!Button)
                Debug.LogError("Button Component not found");

            Reset();
        }

        public virtual void SetChoice(QuestUtilLabelChoice choice)
        {
            this.choice = choice;

            if(!string.IsNullOrEmpty(choice.Text))
            {
                if(text)
                    text.text = choice.Text;
            }   
            else
            {
                text?.gameObject.SetActive(false);
                text?.transform.parent.gameObject.SetActive(false);   
            }

            if(choice.Image)
            {
                if(image)
                    image.sprite = choice.Image;
            }   
            else
            {
                image?.gameObject.SetActive(false);
                image?.transform.parent.gameObject.SetActive(false);   
            }
        }

        public override void OnClick()
        {
            if(choice.IsAnswer)
            {   
                correct.SetActive(true);
                quest?.OnAnswered(true);
            }
            else
            {
                wrong.SetActive(true);
                quest?.OnAnswered(false);
            }
        }

        public override void Disable()
        {
            Button.interactable = false;
        }

        public override void Reset()
        {
            Button.interactable = true;

            if(correct)
                correct.SetActive(false);

            if(wrong)
                wrong.SetActive(false);
        }
    }
}