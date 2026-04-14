using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestWordBuilder : Quest
    {
        [Header("Data")]
        
        [Header("Components")]
        [SerializeField] private Image questionImage;
        [SerializeField] private AudioPlayer questionAudio;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private RectTransform answerContainer;
        [SerializeField] private RectTransform choicesContainer;

        [Header("Prefab")]
        [SerializeField] private QuestWordBuilderItem choiceItemPrefab;

        private SO_QuestWordBuilder dataWordBuilder;
        private QuestWordBuilderItem[] choices;
        
        protected override void Start()
        {
            dataWordBuilder = data as SO_QuestWordBuilder;

            base.Start();

            if(!dataWordBuilder)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestWordBuilder");

            if (questionImage)
            {
                if (dataWordBuilder.Question.Image)
                    questionImage.sprite = dataWordBuilder.Question.Image;
                else
                {
                    questionImage.gameObject.SetActive(false);
                    questionImage.transform.parent.gameObject.SetActive(false);
                }
            }

            if (questionAudio)
            {
                if (dataWordBuilder.Question.Audio)
                    questionAudio.SetAudioClip(dataWordBuilder.Question.Audio);
                else
                {
                    questionAudio.gameObject.SetActive(false);
                    questionAudio.transform.parent.gameObject.SetActive(false);
                }
            }

            if (questionText)
            {
                if (!string.IsNullOrEmpty(dataWordBuilder.Question.Text))
                    questionText.text = dataWordBuilder.Question.Text;
                else
                {
                    questionText.gameObject.SetActive(false);
                    questionText.transform.parent.gameObject.SetActive(false);
                }
            }

            if (choicesContainer && choiceItemPrefab)
            {
                choices = new QuestWordBuilderItem[dataWordBuilder.Values.Length];

                for (int i = 0; i < dataWordBuilder.Values.Length; i++)
                {
                    choices[i] = InstantiateItem(choices[0], i);
                    choices[i].SetChoice(dataWordBuilder.Values[i]);
                }
            }
        }

        QuestWordBuilderItem InstantiateItem(QuestWordBuilderItem prefab, int index)
        {
            QuestWordBuilderItem choiceItem = Instantiate(prefab ? prefab : choiceItemPrefab);

            choiceItem.transform.SetParent(choicesContainer);
            choiceItem.transform.localPosition = Vector3.zero;
            choiceItem.Rect.localScale = Vector3.one;

            return choiceItem;
        }

        public override void Reset()
        {
            foreach (Transform child in answerContainer)
                child.transform.parent = choicesContainer.transform;
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

            Invoke("RecalculateChoiceContainer", 0.5f);
        }
    }
}