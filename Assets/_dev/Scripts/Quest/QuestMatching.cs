using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestMatching : Quest
    {
        [Header("Data")]
        [SerializeField] [FormerlySerializedAs("questionText")] private TMP_Text m_QuestionText;
        [SerializeField] [FormerlySerializedAs("cardsContainer")] private RectTransform m_CardsContainer;
        [SerializeField] [FormerlySerializedAs("success")] private RectTransform m_Success;
        [SerializeField] [FormerlySerializedAs("failed")] private RectTransform m_Failed;

        [Header("Prefab")]
        [SerializeField] [FormerlySerializedAs("cardPrefab")] private QuestMatchingCard m_CardPrefab;

        [Header("Rules")]
        [SerializeField] [FormerlySerializedAs("peekDuration")] private float m_PeekDuration = 1f;

        public float PeekDuration => m_PeekDuration;

        private SO_QuestMatching m_DataQuestMatching;
        private QuestMatchingCard[] m_Cards;
        private int m_UnansweredCorrect = 0;
        private QuestMatchingCard m_OpenedCard = null;

        protected override void Start()
        {
            base.Start();

            m_DataQuestMatching = m_Data as SO_QuestMatching;

            if(!m_DataQuestMatching)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestMatching");

            m_UnansweredCorrect = m_DataQuestMatching.Matches.Length/2;

            if (m_QuestionText)
            {
                if (!string.IsNullOrEmpty(m_DataQuestMatching.Text))
                    m_QuestionText.text = m_DataQuestMatching.Text;
                else
                {
                    m_QuestionText.gameObject.SetActive(false);
                    m_QuestionText.transform.parent.gameObject.SetActive(false);
                }
            }

            if (m_CardsContainer && m_CardPrefab)
            {
                m_Cards = new QuestMatchingCard[m_DataQuestMatching.Matches.Length];

                for (int i = 0; i < m_DataQuestMatching.Matches.Length; i++)
                {
                    m_Cards[i] = InstantiateCard(m_Cards[0]);
                    m_Cards[i].SetMatch(m_DataQuestMatching.Matches[i]);
                }
            }
                
            ResetInternal();
        }

        QuestMatchingCard InstantiateCard(QuestMatchingCard prefab)
        {
            QuestMatchingCard card = Instantiate(prefab ? prefab : m_CardPrefab);

            card.transform.SetParent(m_CardsContainer);
            card.transform.localPosition = Vector3.zero;
            card.Rect.localScale = Vector3.one;

            return card;
        }

        public virtual void Open(QuestMatchingCard card)
        {
            if(m_OpenedCard)
            {
                bool res = m_OpenedCard.Match.Id == card.Match.Id;

                if(res)
                {
                    card.Complete();
                    m_OpenedCard.Complete();

                    m_UnansweredCorrect--;
                    
                    if(m_UnansweredCorrect <= 0)
                        Invoke("Success", m_PeekDuration);
                }
                else
                {
                    foreach (var c in m_Cards)
                        c.Disable();

                    Invoke("CloaseAllCard", m_PeekDuration);
                }

                m_OpenedCard = null;    
            }
            else
                m_OpenedCard = card;
        }

        protected override void ResetInternal()
        {
            m_UnansweredCorrect = m_DataQuestMatching.Matches.Length/2;

            foreach (var card in m_Cards)
                card.Reset();
            
            if(m_Success)
                m_Success.gameObject.SetActive(false);

            if(m_Failed)
                m_Failed.gameObject.SetActive(false);
        }

        void CloaseAllCard()
        {
            foreach (var card in m_Cards)
                card.Close();
        }

        void RecalculateChoiceContainer()
        {
            if (m_Cards[0].Rect.sizeDelta.x > 0 && m_Cards[0].Rect.sizeDelta.y > 0)
            {
                GridLayoutGroup gridLayoutGroup = m_CardsContainer.GetComponent<GridLayoutGroup>();
                if (gridLayoutGroup)
                    gridLayoutGroup.cellSize = m_Cards[0].Rect.sizeDelta;
            }
        }

        void Success()
        {
            if(m_Success)
                m_Success.gameObject.SetActive(true);
            
            base.OnAnswered(true);
        }

        void Failed()
        {
            if(m_Failed)
                m_Failed.gameObject.SetActive(true);
        }

        public override void Enabled()
        {
            base.Enabled();

            Invoke("RecalculateChoiceContainer", 0.5f);
        }
    }
}