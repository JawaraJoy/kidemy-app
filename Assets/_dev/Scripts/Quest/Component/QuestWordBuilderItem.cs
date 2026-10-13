using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestWordBuilderItem : QuestComponent
    {
        [SerializeField] [FormerlySerializedAs("text")] protected TMP_Text m_Text;
        [SerializeField] [FormerlySerializedAs("audioPlayer")] private AudioPlayer m_AudioPlayer;
        [SerializeField] [FormerlySerializedAs("image")] private Image m_Image;
        
        [SerializeField] [FormerlySerializedAs("correct")] protected GameObject m_Correct;
        [SerializeField] [FormerlySerializedAs("wrong")] protected GameObject m_Wrong;
        
        public bool IsRightAnswer { get; private set; } = false;

        protected QuestUtilLabelValue m_Choice;

        protected override void Awake()
        {
            base.Awake();

            if(!Rect)
                Debug.LogError("Gameobject is not UI");

            if(!Button)
                Debug.LogError("Button Component not found");

            Reset();
        }

        public virtual void SetChoice(QuestUtilLabelValue choice)
        {
            this.m_Choice = choice;

            if(!string.IsNullOrEmpty(choice.Text))
            {
                if(m_Text)
                    m_Text.text = choice.Text;
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
                    m_Image.sprite = choice.Image;
            }   
            else
            {
                m_Image?.gameObject.SetActive(false);
                m_Image?.transform.parent.gameObject.SetActive(false);   
            }
        }

        public override void OnClick()
        {
            
        }

        public override void Disable()
        {
            Button.interactable = false;
        }

        public override void Reset()
        {
            Button.interactable = true;

            if(m_Correct)
                m_Correct.SetActive(false);

            if(m_Wrong)
                m_Wrong.SetActive(false);
        }
    }
}