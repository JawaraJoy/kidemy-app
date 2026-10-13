using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMatching : Quest
    {
        [Header("Data")]
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private RectTransform cardsContainer;
        [SerializeField] private RectTransform success;
        [SerializeField] private RectTransform failed;

        [Header("Prefab")]
        [SerializeField] private QuestMatchingCard cardPrefab;

        [Header("Rules")]
        [SerializeField] private float peekDuration = 1f;

        public float PeekDuration => peekDuration;

        private SO_QuestMatching dataQuestMatching;
        private QuestMatchingCard[] cards;
        private int unansweredCorrect = 0;
        private QuestMatchingCard openedCard = null;

        protected override void Start()
        {
            base.Start();

            dataQuestMatching = data as SO_QuestMatching;

            if(!dataQuestMatching)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestMatching");

            unansweredCorrect = dataQuestMatching.Matches.Length/2;

            if (questionText)
            {
                if (!string.IsNullOrEmpty(dataQuestMatching.Text))
                    questionText.text = dataQuestMatching.Text;
                else
                {
                    questionText.gameObject.SetActive(false);
                    questionText.transform.parent.gameObject.SetActive(false);
                }
            }

            if (cardsContainer && cardPrefab)
            {
                cards = new QuestMatchingCard[dataQuestMatching.Matches.Length];

                for (int i = 0; i < dataQuestMatching.Matches.Length; i++)
                {
                    cards[i] = InstantiateCard(cards[0]);
                    cards[i].SetMatch(dataQuestMatching.Matches[i]);
                }
            }
                
            Reset();
        }

        QuestMatchingCard InstantiateCard(QuestMatchingCard prefab)
        {
            QuestMatchingCard card = Instantiate(prefab ? prefab : cardPrefab);

            card.transform.SetParent(cardsContainer);
            card.transform.localPosition = Vector3.zero;
            card.Rect.localScale = Vector3.one;

            return card;
        }

        public virtual void Open(QuestMatchingCard card)
        {
            if(openedCard)
            {
                bool res = openedCard.Match.Id == card.Match.Id;

                if(res)
                {
                    card.Complete();
                    openedCard.Complete();

                    unansweredCorrect--;
                    
                    if(unansweredCorrect <= 0)
                        Invoke("Success", peekDuration);
                }
                else
                {
                    foreach (var c in cards)
                        c.Disable();

                    Invoke("CloaseAllCard", peekDuration);
                }

                openedCard = null;    
            }
            else
                openedCard = card;
        }

        public override void Reset()
        {
            unansweredCorrect = dataQuestMatching.Matches.Length/2;

            foreach (var card in cards)
                card.Reset();
            
            if(success)
                success.gameObject.SetActive(false);

            if(failed)
                failed.gameObject.SetActive(false);
        }

        void CloaseAllCard()
        {
            foreach (var card in cards)
                card.Close();
        }

        void RecalculateChoiceContainer()
        {
            if (cards[0].Rect.sizeDelta.x > 0 && cards[0].Rect.sizeDelta.y > 0)
            {
                GridLayoutGroup gridLayoutGroup = cardsContainer.GetComponent<GridLayoutGroup>();
                if (gridLayoutGroup)
                    gridLayoutGroup.cellSize = cards[0].Rect.sizeDelta;
            }
        }

        void Success()
        {
            if(success)
                success.gameObject.SetActive(true);
            
            base.OnAnswered(true);
        }

        void Failed()
        {
            if(failed)
                failed.gameObject.SetActive(true);
        }

        public override void Enabled()
        {
            base.Enabled();

            Invoke("RecalculateChoiceContainer", 0.5f);
        }
    }
}