using EasyTextEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestMultipleChoicesItem : QuestComponent
    {
        [SerializeField] [FormerlySerializedAs("text")] protected TMP_Text m_Text;
        [SerializeField] [FormerlySerializedAs("audioPlayer")] protected AudioPlayer m_AudioPlayer;
        [SerializeField] [FormerlySerializedAs("image")] protected Image m_Image;
        [SerializeField] [FormerlySerializedAs("tint")] protected Image m_Tint;
        [SerializeField] [FormerlySerializedAs("correct")] protected GameObject m_Correct;
        [SerializeField] [FormerlySerializedAs("wrong")] protected GameObject m_Wrong;
        
        private bool m_IsRightAnswer = false;
        public bool IsRightAnswer => m_IsRightAnswer;

        protected QuestUtilLabelChoice m_Choice;
        protected ButtonEvents m_Events;

        private TextEffect m_TextEffect;
        private FeedbackManager m_FeedbackManager;
        private bool m_IsFeedbackBound = false;
        
        protected override void Awake()
        {
            base.Awake();

            if(!Rect)
                Debug.LogError("Gameobject is not UI");

            if(!Button)
                Debug.LogError("Button Component not found");

            ResetInternal();
        }

        protected override void InitInternal()
        {
            base.InitInternal();

            TryGetComponent<ButtonEvents>(out m_Events);
            
            if(m_Events && m_FeedbackManager && !m_IsFeedbackBound)
            {
                m_IsFeedbackBound = true;
                m_Events.AddEventOnPointerOver(m_FeedbackManager.ButtonOver.Play);
                m_Events.AddEventOnPointerExit(m_FeedbackManager.ButtonExit.Play);
                m_Events.AddEventOnPointerClick(m_FeedbackManager.ButtonClick.Play);
            }
        }

        public void SetFeedbackManager(FeedbackManager feedbackManager)
        {
            SetFeedbackManagerInternal(feedbackManager);
        }

        protected virtual void SetFeedbackManagerInternal(FeedbackManager feedbackManager)
        {
            m_FeedbackManager = feedbackManager;

            InitInternal();
        }

        public virtual void SetChoice(QuestUtilLabelChoice choice)
        {
            this.m_Choice = choice;

            if(!string.IsNullOrEmpty(choice.Text))
            {
                if(m_Text)
                {
                    m_Text.text = choice.Text;
                    if (m_TextEffect == null)
                    {
                        if (m_Text.TryGetComponent(out TextEffect effect))
                        {
                            m_TextEffect = effect;
                        }
                    }
                    if (m_TextEffect)
                    {
                        m_TextEffect.Refresh();
                    }
                }

                
                    
                if (choice.UseTextColor)
                {
                    m_Text.color = choice.TextColor;
                }
            }   
            else
            {
                m_Text?.gameObject.SetActive(false);
                m_Text?.transform.parent.gameObject.SetActive(false);   
            }

            if(choice.Audio)
            {
                if(m_AudioPlayer)
                    m_AudioPlayer.SetAudioClip(choice.Audio);
            }   
            else
            {
                m_AudioPlayer?.gameObject.SetActive(false);
                m_AudioPlayer?.transform.parent.gameObject.SetActive(false);   
            }
            if(choice.Image)
            {
                if(m_Image)   
                {
                    m_Image.sprite = choice.Image;
                    m_Image.color = choice.ImageColor;
                }
            }   
            else
            {
                m_Image?.gameObject.SetActive(false);
                m_Image?.transform.parent.gameObject.SetActive(false);   
            }

            if (choice.Color != Color.white && choice.Color != Color.black)
            {
                
            }
            if (m_Tint)
                m_Tint.color = choice.Color;
        }

        public override void OnClick()
        {
            DisableInternal();

            if(m_Choice.IsAnswer)
                m_Quest?.OnAnswered(true);
            else
                m_Quest?.OnAnswered(false);
        }

        protected override void DisableInternal()
        {
            Button.interactable = false;
        }

        protected override void ResetInternal()
        {
            Button.interactable = true;

            if(m_Correct)
                m_Correct.SetActive(false);

            if(m_Wrong)
                m_Wrong.SetActive(false);
        }
    }
}