using EasyTextEffects;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestMultipleChoices : Quest
    {
        [Header("Data")]
        
        [Header("Components")]
        [SerializeField] 
        private RectTransform m_MultipleImagesQuestionContainer;
        [SerializeField] [FormerlySerializedAs("questionImage")] private Image m_QuestionImage;
        [SerializeField] [FormerlySerializedAs("questionAudio")] private AudioPlayer m_QuestionAudio;
        [SerializeField] [FormerlySerializedAs("questionText")] private TMP_Text m_QuestionText;
        [SerializeField]
        private TextMeshProUGUI m_QuestName;
        [SerializeField] [FormerlySerializedAs("choicesContainer")] private RectTransform m_ChoicesContainer;
        [SerializeField]
        private AnimationHandler m_QuestAnimationHandler;

        [Header("Prefab")]
        [SerializeField] [FormerlySerializedAs("choiceItemPrefab")] private QuestMultipleChoicesItem m_ChoiceItemPrefab;

        private SO_QuestMultipleChoice m_DataMultipleChoice;
        private QuestMultipleChoicesItem[] m_Choices;
        public AnimationHandler QuestAnimationHandler => m_QuestAnimationHandler;
        private int m_UnansweredCorrect = 0;

        private VoiceRequest m_VoiceQuestionRequest = new VoiceRequest();

        private List<Image> m_SpawnedmultipleImages = new List<Image>();

        private QuestMultipleChoicesItem m_CorrectChoice;

        private TextEffect m_TextEffect;
        protected override void Start()
        {
            base.Start();
            
            if(!m_DataMultipleChoice)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestMultipleChoice");

            m_UnansweredCorrect = m_DataMultipleChoice.TotalAnswer;

            // using the new multiple images question system
            if (m_Data.HasMultipleImages(out var multipleImages, out var prefab) && m_MultipleImagesQuestionContainer)
            {
                m_MultipleImagesQuestionContainer.gameObject.SetActive(true);
                m_QuestionImage.gameObject.SetActive(false);
                m_QuestionImage.transform.parent.gameObject.SetActive(false);
                foreach (var image in m_SpawnedmultipleImages)
                {
                    image.gameObject.SetActive(false);
                }
                for (int i = 0; i < multipleImages.Length; i++)
                {
                    if (i < m_SpawnedmultipleImages.Count)
                    {
                        m_SpawnedmultipleImages[i].sprite = multipleImages[i];
                    }
                    else
                    {
                        Image image = Instantiate(prefab, m_MultipleImagesQuestionContainer);
                        image.sprite = multipleImages[i];
                        m_SpawnedmultipleImages.Add(image);
                    }
                    m_SpawnedmultipleImages[i].gameObject.SetActive(true);
                }
            }
            else
            {
                if (m_MultipleImagesQuestionContainer)
                {
                    m_MultipleImagesQuestionContainer.gameObject.SetActive(false);
                }
                // old code for single image question
                if (m_QuestionImage)
                {
                    m_QuestionImage.gameObject.SetActive(true);
                    if (m_DataMultipleChoice.Question.Image)
                        m_QuestionImage.sprite = m_DataMultipleChoice.Question.Image;
                    else
                    {
                        m_QuestionImage.gameObject.SetActive(false);
                        m_QuestionImage.transform.parent.gameObject.SetActive(false);
                    }
                }

            }
            

            if (m_QuestionAudio)
            {
                if (m_DataMultipleChoice.Question.Audio)
                    m_QuestionAudio.SetAudioClip(m_DataMultipleChoice.Question.Audio);
                else
                {
                    m_QuestionAudio.gameObject.SetActive(false);
                    m_QuestionAudio.transform.parent.gameObject.SetActive(false);
                }
            }

            if (m_QuestionText)
            {
                if (m_DataMultipleChoice.Question.FormatedText)
                {
                    if (m_QuestionText.TryGetComponent(out TextEffect textEff))
                    {
                        m_TextEffect = textEff;
                    }
                    m_TextEffect.preset = m_DataMultipleChoice.Question.FormatedText.EffectPreset;
                    m_QuestionText.text = m_DataMultipleChoice.Question.FormatedText.GetFormattedText();
                    m_TextEffect.Refresh();
                }
                else
                {
                    // we can control color with formated text, maybe we should remove this option in the future
                    if (m_DataMultipleChoice.Question.UseTextColor)
                    {
                        m_QuestionText.color = m_DataMultipleChoice.Question.TextColor;
                    }
                    if (!string.IsNullOrEmpty(m_DataMultipleChoice.Question.Text))
                        m_QuestionText.text = m_DataMultipleChoice.Question.Text;
                    else
                    {
                        m_QuestionText.gameObject.SetActive(false);
                        m_QuestionText.transform.parent.gameObject.SetActive(false);
                    }
                }
                
                
            }
            if (m_Challenged)
            {
                if (m_Challenged.ChoicePrefab is QuestMultipleChoicesItem choicePrefab)
                {
                    m_ChoicesContainer.gameObject.SetActive(true);
                    m_Choices = new QuestMultipleChoicesItem[m_DataMultipleChoice.Choices.Length];
                    for (int i = 0; i < m_DataMultipleChoice.Choices.Length; i++)
                    {
                        m_Choices[i] = InstantiateItem(choicePrefab, i);
                        m_Choices[i].SetChoice(m_DataMultipleChoice.Choices[i]);

                        if(m_DataMultipleChoice.Choices[i].IsAnswer)
                            m_CorrectChoice = m_Choices[i];
                    
                    }
                }
                if (m_QuestName)
                {
                    m_QuestName.text = m_DataMultipleChoice.Question.Text;
                }
            }
            else
            {
                if (m_ChoicesContainer && m_ChoiceItemPrefab)
                {
                    m_ChoicesContainer.gameObject.SetActive(true);

                    m_Choices = new QuestMultipleChoicesItem[m_DataMultipleChoice.Choices.Length];

                    for (int i = 0; i < m_DataMultipleChoice.Choices.Length; i++)
                    {
                        m_Choices[i] = InstantiateItem(m_Choices[0], i);
                        m_Choices[i].SetChoice(m_DataMultipleChoice.Choices[i]);

                        if(m_DataMultipleChoice.Choices[i].IsAnswer)
                            m_CorrectChoice = m_Choices[i];
                    }
                }
            }
            


            SetDialog();
        }

        protected override void SetupInternal()
        {
            m_DataMultipleChoice = m_Data as SO_QuestMultipleChoice;

            base.SetupInternal();
        }

        protected override void PlayQuestionVoiceInternal()
        {
            base.PlayQuestionVoiceInternal();

            StopAllCoroutines();

            StartCoroutine(PlayChoicesVoiceCO());
        }

        IEnumerator PlayChoicesVoiceCO()
        {
            yield return new WaitWhile(() => m_AudioSource.isPlaying);

            yield return new WaitForSeconds(1);

            foreach (var choice in m_DataMultipleChoice.Choices)
            {
                yield return new WaitWhile(() => m_AudioSource.isPlaying);

                if(choice.Audio)
                {
                    yield return new WaitForSeconds(0.5f);

                    m_AudioSource.PlayOneShot(choice.Audio);
                }
            }

            yield return null;
        }

        QuestMultipleChoicesItem InstantiateItem(QuestMultipleChoicesItem prefab, int index)
        {
            QuestMultipleChoicesItem choiceItem = Instantiate(prefab ? prefab : m_ChoiceItemPrefab);

            choiceItem.transform.SetParent(m_ChoicesContainer);
            choiceItem.transform.localPosition = Vector3.zero;
            choiceItem.Init();
            choiceItem.Rect.localScale = Vector3.one;
            choiceItem.gameObject.SetActive(true);

            return choiceItem;
        }

        public override void OnAnswered(bool result, bool submit = false)
        {
            bool isFinished = true;

            if (result == true)
            {
                m_UnansweredCorrect--;

                isFinished = m_UnansweredCorrect <= 0;
            }

            if (isFinished)
            {
                foreach (var choice in m_Choices)
                    choice.Disable();

                SubmitInternal();
            }
        }

        protected override void SubmitInternal(int star = 1)
        {
            if(m_UnansweredCorrect == 0)
                star = 3;

            StopAllCoroutines();

            GameManager.Instance.Submit(star);
        }

        protected override void ResetInternal()
        {
            m_UnansweredCorrect = m_DataMultipleChoice.TotalAnswer;

            if(m_Choices != null)
            {
                foreach (var choice in m_Choices)
                    choice.Reset();
            }
                
            SetDialog();

            base.ResetInternal();
        }

        void RecalculateChoiceContainer()
        {
            if (m_Choices[0].Rect.sizeDelta.x > 0 && m_Choices[0].Rect.sizeDelta.y > 0)
            {
                m_ChoicesContainer.TryGetComponent<GridLayoutGroup>(out GridLayoutGroup gridLayoutGroup);
                if (gridLayoutGroup)
                    gridLayoutGroup.cellSize = m_Choices[0].Rect.sizeDelta;
            }
        }

        public override void Enabled()
        {
            base.Enabled();

            SetDialog();

            // use name of method to avoid hardcoding string
            Invoke(nameof(RecalculateChoiceContainer), 0.5f);
        }

        void SetDialog()
        {
            
            if (!string.IsNullOrEmpty(m_DataMultipleChoice.Question.Text))
            {
                string dialog = m_DataMultipleChoice.Question.Text;
                if (m_DataMultipleChoice.Question.FormatedText)
                {
                    dialog = m_DataMultipleChoice.Question.FormatedText.GetFormattedText();
                    
                }
                GameManager.Instance.SetNPCDialog(dialog);
            }
            if (m_Challenged)
            {
                if (m_DataMultipleChoice.HowManySoundConfig && m_QuestAnimationHandler)
                {
                    m_QuestAnimationHandler.SetClip(m_DataMultipleChoice.HowManySoundConfig);
                }
            }
        }

        /// <summary>
        /// Returns all VoiceRequests needed by this quest.
        /// Reads question text from 'data' and voice_id from 'character'.
        /// </summary>
        public override VoiceRequest[] GetVoiceRequests()
        {
            SetupInternal();
            return m_DataMultipleChoice != null
                ? m_DataMultipleChoice.GetVoiceRequests()
                : Array.Empty<VoiceRequest>();
        }

        /// <summary>
        /// Checks if a specific voice request already has an AudioClip assigned in 'data'.
        /// </summary>
        public override bool HasVoiceClip(string requestId)
        {
            return m_DataMultipleChoice != null && m_DataMultipleChoice.HasVoiceClip(requestId);
        }

        /// <summary>
        /// Assigns the downloaded & imported AudioClip directly to the SO_Quest referenced in 'data'.
        /// </summary>
        public override void AssignVoiceClip(string requestId, AudioClip clip)
        {
            if (m_DataMultipleChoice != null)
                m_DataMultipleChoice.AssignVoiceClip(requestId, clip);
        }

        public override void ShowTutorial()
        {
            GameManager.Instance.ShowTutorial(m_CorrectChoice ? m_CorrectChoice.Rect : m_Choices[0].Rect);
        }
    }
}
