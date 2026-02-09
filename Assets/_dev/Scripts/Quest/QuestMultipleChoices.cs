using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMultipleChoices : Quest
    {
        [Header("Data")]
        [SerializeField] private SO_QuestMultipleChoice multipleChoice;

        [Header("Components")]
        [SerializeField] private Image questionImage;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private RectTransform choicesContainer;

        [Header("Prefab")]
        [SerializeField] private QuestMultipleChoicesItem choiceItemPrefab;

        private QuestMultipleChoicesItem[] choices;
        private int unansweredCorrect = 0;

        protected override void Start()
        {
            base.Start();

            unansweredCorrect = multipleChoice.TotalAnswer;

            if (questionImage)
            {
                if (multipleChoice.Question.Image)
                    questionImage.sprite = multipleChoice.Question.Image;
                else
                {
                    questionImage.gameObject.SetActive(false);
                    questionImage.transform.parent.gameObject.SetActive(false);
                }
            }

            if (questionText)
            {
                if (!string.IsNullOrEmpty(multipleChoice.Question.Text))
                    questionText.text = multipleChoice.Question.Text;
                else
                {
                    questionText.gameObject.SetActive(false);
                    questionText.transform.parent.gameObject.SetActive(false);
                }
            }

            if (choicesContainer && choiceItemPrefab)
            {
                choices = new QuestMultipleChoicesItem[multipleChoice.Choices.Length];

                for (int i = 0; i < multipleChoice.Choices.Length; i++)
                {
                    choices[i] = InstantiateItem(choices[0], i);
                    choices[i].SetChoice(multipleChoice.Choices[i]);
                }

                Invoke("RecalculateChoiceContainer", 0.5f);
            }

            if(submitButton)
                submitButton.gameObject.SetActive(false);
        }

        QuestMultipleChoicesItem InstantiateItem(QuestMultipleChoicesItem prefab, int index)
        {
            QuestMultipleChoicesItem choiceItem = Instantiate(prefab ? prefab : choiceItemPrefab);

            choiceItem.transform.SetParent(choicesContainer);
            choiceItem.transform.localPosition = Vector3.zero;
            choiceItem.Rect.localScale = Vector3.one;

            return choiceItem;
        }

        public override void OnAnswered(bool result)
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

                if (restartButton)
                    restartButton.gameObject.SetActive(true);
            }
        }

        public override void Reset()
        {
            unansweredCorrect = multipleChoice.TotalAnswer;

            foreach (var choice in choices)
                choice.Reset();

            if (restartButton)
                restartButton.gameObject.SetActive(false);
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
    }
}