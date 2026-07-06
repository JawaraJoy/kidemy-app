using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMultipleChoicesItem : QuestComponent
    {
        [SerializeField] protected TMP_Text text;
        [SerializeField] protected AudioPlayer audioPlayer;
        [SerializeField] protected Image image;
        [SerializeField] protected Image tint;
        [SerializeField] protected GameObject correct;
        [SerializeField] protected GameObject wrong;
        
        public bool IsRightAnswer { get; private set; } = false;

        protected QuestUtilLabelChoice choice;
        protected ButtonEvents events;
        
        protected override void Awake()
        {
            base.Awake();

            if(!Rect)
                Debug.LogError("Gameobject is not UI");

            if(!Button)
                Debug.LogError("Button Component not found");

            Reset();
        }

        public override void Init()
        {
            base.Init();

            events = GetComponent<ButtonEvents> ();

            if(events)
            {
                events.AddEventOnPointerOver(FeedbackManager.Instance.ButtonOver.Play);
                events.AddEventOnPointerExit(FeedbackManager.Instance.ButtonExit.Play);
                events.AddEventOnPointerClick(FeedbackManager.Instance.ButtonClick.Play);
            }
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

            if(choice.Audio)
            {
                if(audioPlayer)
                    audioPlayer.SetAudioClip(choice.Audio);
            }   
            else
            {
                audioPlayer?.gameObject.SetActive(false);
                audioPlayer?.transform.parent.gameObject.SetActive(false);   
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

            if(choice.Color != Color.white && choice.Color != Color.black)
            {
                if(tint)
                    tint.color = choice.Color;
            }   
        }

        public override void OnClick()
        {
            Disable();

            if(choice.IsAnswer)
                quest?.OnAnswered(true);
            else
                quest?.OnAnswered(false);
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