using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestWordBuilder : Quest
    {
        private const string c_RecalculateChoiceContainerMethod = "RecalculateChoiceContainer";

        [Header("Data")]
        
        [Header("Components")]
        [SerializeField] [FormerlySerializedAs("questionImage")] private Image m_QuestionImage;
        [SerializeField] [FormerlySerializedAs("questionAudio")] private AudioPlayer m_QuestionAudio;
        [SerializeField] [FormerlySerializedAs("questionText")] private TMP_Text m_QuestionText;
        [SerializeField] [FormerlySerializedAs("answerContainer")] private RectTransform m_AnswerContainer;
        [SerializeField] [FormerlySerializedAs("choicesContainer")] private RectTransform m_ChoicesContainer;

        [Header("Prefab")]
        [SerializeField] [FormerlySerializedAs("choiceItemPrefab")] private QuestWordBuilderItem m_ChoiceItemPrefab;

        private SO_QuestWordBuilder m_DataWordBuilder;
        private QuestWordBuilderItem[] m_Choices;
        
        protected override void Start()
        {
            m_DataWordBuilder = m_Data as SO_QuestWordBuilder;

            base.Start();

            if(!m_DataWordBuilder)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestWordBuilder");

            if (m_QuestionImage)
            {
                if (m_DataWordBuilder.Question.Image)
                    m_QuestionImage.sprite = m_DataWordBuilder.Question.Image;
                else
                {
                    m_QuestionImage.gameObject.SetActive(false);
                    m_QuestionImage.transform.parent.gameObject.SetActive(false);
                }
            }

            if (m_QuestionAudio)
            {
                if (m_DataWordBuilder.Question.Audio)
                    m_QuestionAudio.SetAudioClip(m_DataWordBuilder.Question.Audio);
                else
                {
                    m_QuestionAudio.gameObject.SetActive(false);
                    m_QuestionAudio.transform.parent.gameObject.SetActive(false);
                }
            }

            if (m_QuestionText)
            {
                if (!string.IsNullOrEmpty(m_DataWordBuilder.Question.Text))
                    m_QuestionText.text = m_DataWordBuilder.Question.Text;
                else
                {
                    m_QuestionText.gameObject.SetActive(false);
                    m_QuestionText.transform.parent.gameObject.SetActive(false);
                }
            }

            if (m_ChoicesContainer && m_ChoiceItemPrefab)
            {
                m_Choices = new QuestWordBuilderItem[m_DataWordBuilder.Values.Length];

                for (int i = 0; i < m_DataWordBuilder.Values.Length; i++)
                {
                    m_Choices[i] = InstantiateItem(m_Choices[0], i);
                    m_Choices[i].SetChoice(m_DataWordBuilder.Values[i]);
                }
            }
        }

        QuestWordBuilderItem InstantiateItem(QuestWordBuilderItem prefab, int index)
        {
            QuestWordBuilderItem choiceItem = Instantiate(prefab ? prefab : m_ChoiceItemPrefab);

            choiceItem.transform.SetParent(m_ChoicesContainer);
            choiceItem.transform.localPosition = Vector3.zero;
            choiceItem.Rect.localScale = Vector3.one;

            return choiceItem;
        }

        protected override void ResetInternal()
        {
            foreach (Transform child in m_AnswerContainer)
                child.transform.parent = m_ChoicesContainer.transform;
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

            Invoke(c_RecalculateChoiceContainerMethod, 0.5f);
        }
    }
}