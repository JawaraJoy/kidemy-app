using EasyTextEffects;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMultipleChoices : Quest
    {
        [Header("Data")]
        
        [Header("Components")]
        [SerializeField] 
        private RectTransform m_MultipleImagesQuestionContainer;
        [SerializeField] private Image questionImage;
        [SerializeField] private AudioPlayer questionAudio;
        [SerializeField] private TMP_Text questionText;
        [SerializeField]
        private TextMeshProUGUI m_QuestName;
        [SerializeField] private RectTransform choicesContainer;
        [SerializeField]
        private AnimationHandler m_QuestAnimationHandler;

        [Header("Prefab")]
        [SerializeField] private QuestMultipleChoicesItem choiceItemPrefab;

        private SO_QuestMultipleChoice dataMultipleChoice;
        private QuestMultipleChoicesItem[] choices;
        public AnimationHandler QuestAnimationHandler => m_QuestAnimationHandler;
        private int unansweredCorrect = 0;

        private VoiceRequest voiceQuestionRequest = new VoiceRequest();

        private List<Image> m_SpawnedmultipleImages = new List<Image>();

        private TextEffect m_TextEffect;
        protected override void Start()
        {
            base.Start();
            
            if(!dataMultipleChoice)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestMultipleChoice");

            unansweredCorrect = dataMultipleChoice.TotalAnswer;

            // using the new multiple images question system
            if (data.HasMultipleImages(out var multipleImages, out var prefab) && m_MultipleImagesQuestionContainer)
            {
                m_MultipleImagesQuestionContainer.gameObject.SetActive(true);
                questionImage.gameObject.SetActive(false);
                questionImage.transform.parent.gameObject.SetActive(false);
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
                if (questionImage)
                {
                    questionImage.gameObject.SetActive(true);
                    if (dataMultipleChoice.Question.Image)
                        questionImage.sprite = dataMultipleChoice.Question.Image;
                    else
                    {
                        questionImage.gameObject.SetActive(false);
                        questionImage.transform.parent.gameObject.SetActive(false);
                    }
                }

            }
            

            if (questionAudio)
            {
                if (dataMultipleChoice.Question.Audio)
                    questionAudio.SetAudioClip(dataMultipleChoice.Question.Audio);
                else
                {
                    questionAudio.gameObject.SetActive(false);
                    questionAudio.transform.parent.gameObject.SetActive(false);
                }
            }

            if (questionText)
            {
                if (dataMultipleChoice.Question.FormatedText)
                {
                    if (questionText.TryGetComponent(out TextEffect textEff))
                    {
                        m_TextEffect = textEff;
                    }
                    m_TextEffect.preset = dataMultipleChoice.Question.FormatedText.EffectPreset;
                    questionText.text = dataMultipleChoice.Question.FormatedText.GetFormattedText();
                    m_TextEffect.Refresh();
                }
                else
                {
                    // we can control color with formated text, maybe we should remove this option in the future
                    if (dataMultipleChoice.Question.UseTextColor)
                    {
                        questionText.color = dataMultipleChoice.Question.TextColor;
                    }
                    if (!string.IsNullOrEmpty(dataMultipleChoice.Question.Text))
                        questionText.text = dataMultipleChoice.Question.Text;
                    else
                    {
                        questionText.gameObject.SetActive(false);
                        questionText.transform.parent.gameObject.SetActive(false);
                    }
                }
                
                
            }
            if (m_Challenged)
            {
                if (m_Challenged.ChoicePrefab is QuestMultipleChoicesItem choicePrefab)
                {
                    choicesContainer.gameObject.SetActive(true);
                    choices = new QuestMultipleChoicesItem[dataMultipleChoice.Choices.Length];
                    for (int i = 0; i < dataMultipleChoice.Choices.Length; i++)
                    {
                        choices[i] = InstantiateItem(choicePrefab, i);
                        choices[i].SetChoice(dataMultipleChoice.Choices[i]);
                    }
                }
                if (m_QuestName)
                {
                    m_QuestName.text = dataMultipleChoice.Question.Text;
                }
            }
            else
            {
                if (choicesContainer && choiceItemPrefab)
                {
                    choicesContainer.gameObject.SetActive(true);

                    choices = new QuestMultipleChoicesItem[dataMultipleChoice.Choices.Length];

                    for (int i = 0; i < dataMultipleChoice.Choices.Length; i++)
                    {
                        choices[i] = InstantiateItem(choices[0], i);
                        choices[i].SetChoice(dataMultipleChoice.Choices[i]);
                    }
                }
            }
            


            SetDialog();
        }

        public override void Setup()
        {
            dataMultipleChoice = data as SO_QuestMultipleChoice;

            base.Setup();
        }

        public override void PlayQuestionVoice()
        {
            base.PlayQuestionVoice();

            StopAllCoroutines();

            StartCoroutine(PlayChoicesVoiceCO());
        }

        IEnumerator PlayChoicesVoiceCO()
        {
            yield return new WaitWhile(() => audioSource.isPlaying);

            yield return new WaitForSeconds(1);

            foreach (var choice in dataMultipleChoice.Choices)
            {
                yield return new WaitWhile(() => audioSource.isPlaying);

                if(choice.Audio)
                {
                    yield return new WaitForSeconds(0.5f);

                    audioSource.PlayOneShot(choice.Audio);
                }
            }

            yield return null;
        }

        QuestMultipleChoicesItem InstantiateItem(QuestMultipleChoicesItem prefab, int index)
        {
            QuestMultipleChoicesItem choiceItem = Instantiate(prefab ? prefab : choiceItemPrefab);

            choiceItem.transform.SetParent(choicesContainer);
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
                unansweredCorrect--;

                isFinished = unansweredCorrect <= 0;
            }

            if (isFinished)
            {
                foreach (var choice in choices)
                    choice.Disable();

                Submit();
            }
        }

        public override void Submit(int star = 1)
        {
            if(unansweredCorrect == 0)
                star = 3;

            StopAllCoroutines();

            GameManager.Instance.Submit(star);
        }

        public override void Reset()
        {
            unansweredCorrect = dataMultipleChoice.TotalAnswer;

            if(choices != null)
            {
                foreach (var choice in choices)
                    choice.Reset();
            }
                
            SetDialog();

            base.Reset();
        }

        void RecalculateChoiceContainer()
        {
            if (choices[0].Rect.sizeDelta.x > 0 && choices[0].Rect.sizeDelta.y > 0)
            {
                GridLayoutGroup gridLayoutGroup = choicesContainer.GetComponent<GridLayoutGroup>();
                if (gridLayoutGroup)
                    gridLayoutGroup.cellSize = choices[0].Rect.sizeDelta;
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
            
            if (!string.IsNullOrEmpty(dataMultipleChoice.Question.Text))
            {
                string dialog = dataMultipleChoice.Question.Text;
                if (dataMultipleChoice.Question.FormatedText)
                {
                    dialog = dataMultipleChoice.Question.FormatedText.GetFormattedText();
                    
                }
                GameManager.Instance.SetNPCDialog(dialog);
            }
            if (m_Challenged)
            {
                if (dataMultipleChoice.HowManySoundConfig && m_QuestAnimationHandler)
                {
                    m_QuestAnimationHandler.SetClip(dataMultipleChoice.HowManySoundConfig);
                }
            }
        }

        /// <summary>
        /// Returns all VoiceRequests needed by this quest.
        /// Reads question text from 'data' and voice_id from 'character'.
        /// </summary>
        public override VoiceRequest[] GetVoiceRequests()
        {
            Setup();

            if (dataMultipleChoice != null && dataMultipleChoice.Choices.Length > 0)
            {
                List<VoiceRequest> voiceRequests = new List<VoiceRequest>();
                
                VoiceRequest[] baseVoiceRequest = base.GetVoiceRequests();

                if(baseVoiceRequest.Length > 0)
                    voiceRequests.Add(baseVoiceRequest[0]);

                for (int i = 0; i < dataMultipleChoice.Choices.Length; i++)
                {
                    if(!string.IsNullOrEmpty(dataMultipleChoice.Choices[i].Text))
                    {
                        voiceRequests.Add(new VoiceRequest
                        {
                            id = name + "_choice_" + i,
                            text = dataMultipleChoice.Choices[i].Text,
                            voice_id = GetVoiceId()
                        });
                    }   
                }

                return voiceRequests.ToArray();
            }

            return Array.Empty<VoiceRequest>();
        }

        /// <summary>
        /// Checks if a specific voice request already has an AudioClip assigned in 'data'.
        /// </summary>
        public override bool HasVoiceClip(string requestId)
        {
            if (requestId.IndexOf("_choice_") > 0)
            {   
                int index = StringHelper.ExtractId(requestId);

                if(index >= 0)
                    return dataMultipleChoice.Choices[index] != null && dataMultipleChoice.Choices[index].Audio != null;
            }
            else if (requestId.IndexOf("_question") > 0)
                return dataMultipleChoice.Question.Audio != null;
            
            return false;
        }

        /// <summary>
        /// Assigns the downloaded & imported AudioClip directly to the SO_Quest referenced in 'data'.
        /// </summary>
        public override void AssignVoiceClip(string requestId, AudioClip clip)
        {
            if (requestId.IndexOf("_choice_") > 0)
            {

                int index = StringHelper.ExtractId(requestId);

                if(dataMultipleChoice.Choices[index] != null)
                    dataMultipleChoice.Choices[index].SetAudio(clip);
            }
            else if (requestId.IndexOf("_question") > 0)
                data.Question.SetAudio(clip);
        }
    }
}