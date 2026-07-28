using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMultipleChoices : Quest
    {
        [Header("Data")]
        
        [Header("Components")]
        [SerializeField] private Image questionImage;
        [SerializeField] private AudioPlayer questionAudio;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private RectTransform choicesContainer;

        [Header("Prefab")]
        [SerializeField] private QuestMultipleChoicesItem choiceItemPrefab;

        private SO_QuestMultipleChoice dataMultipleChoice;
        private QuestMultipleChoicesItem[] choices;
        private int unansweredCorrect = 0;

        private VoiceRequest voiceQuestionRequest = new VoiceRequest();

        protected override void Start()
        {
            base.Start();
            
            if(!dataMultipleChoice)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestMultipleChoice");

            unansweredCorrect = dataMultipleChoice.TotalAnswer;

            if (questionImage)
            {
                if (dataMultipleChoice.Question.Image)
                    questionImage.sprite = dataMultipleChoice.Question.Image;
                else
                {
                    questionImage.gameObject.SetActive(false);
                    questionImage.transform.parent.gameObject.SetActive(false);
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
                if (!string.IsNullOrEmpty(dataMultipleChoice.Question.Text))
                    questionText.text = dataMultipleChoice.Question.Text;
                else
                {
                    questionText.gameObject.SetActive(false);
                    questionText.transform.parent.gameObject.SetActive(false);
                }
            }

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

            SetDialog();
        }

        public override void Setup()
        {
            dataMultipleChoice = data as SO_QuestMultipleChoice;

            Debug.Log(dataMultipleChoice);

            voiceQuestionRequest.id = dataMultipleChoice.name + "_question";
            voiceQuestionRequest.text = dataMultipleChoice.Question.Text;
            voiceQuestionRequest.voice_id = character ? character.CharacterId : "";

            GameManager.Instance.Asset.AddVoiceRequest(voiceQuestionRequest);
        }

        public override void PlayQuestionVoice()
        {
            AssetManager.Instance.GetCachedAudio(
                voiceId: voiceQuestionRequest.id,
                onSuccess: (clip) =>
                {
                    GameManager.Instance.AudioSource.PlayOneShot(clip);

                    Debug.Log($"[Quest] Playing voice clip for ID: {dataMultipleChoice.name + "_question"}");
                },
                onError: (error) =>
                {
                    Debug.LogError($"[Quest] Failed to play voice for ID '{dataMultipleChoice.name + "_question"}': {error}");
                }
            );
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

            GameManager.Instance.Submit(star);
        }

        public override void Reset()
        {
            unansweredCorrect = dataMultipleChoice.TotalAnswer;

            foreach (var choice in choices)
                choice.Reset();

            SetDialog();
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

            Invoke("RecalculateChoiceContainer", 0.5f);
        }

        void SetDialog()
        {
            if (!string.IsNullOrEmpty(dataMultipleChoice.Question.Text))
                GameManager.Instance.SetNPCDialog(dataMultipleChoice.Question.Text);
        }
    }
}