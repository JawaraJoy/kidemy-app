using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMatchingCard : QuestComponent
    {
        public QuestUtilLabelMatching Match => match;

        private Sprite cardFace;
        private Sprite cardBack;

        private QuestMatching matching;
        private QuestUtilLabelMatching match;

        private bool isComplete;

        protected override void Start()
        {
            matching = GetComponentInParent<QuestMatching>();

            if(matching)
                quest = matching;
            else
                Debug.LogError("QuestMatching Component not found");   

            if(Image)
                cardBack = Image.sprite;
            else
                Debug.LogError("Image Component not found");            
        }

        public virtual void SetMatch(QuestUtilLabelMatching match)
        {
            this.match = match;

            cardFace = match.Image;
        }

        public override void OnClick()
        {
            Image.sprite = cardFace;

            matching.Open(this);
        }

        public virtual void Close()
        {
            if(!isComplete)
            {
                if(cardBack)
                    Image.sprite = cardBack;
                Button.interactable = true;
            }
        }

        public override void Disable()
        {
            Button.interactable = false;
        }

        public virtual void Complete()
        {
            isComplete = true;

            Button.interactable = false;

            Invoke("Hide", matching.PeekDuration);
        }

        public virtual void Hide()
        {
            Image.enabled = false;
        }

        public override void Reset()
        {
            isComplete = false;

            Button.interactable = true;

            Close();
        }
    }
}