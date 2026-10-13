using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMatchingCard : QuestComponent
    {
        public QuestUtilLabelMatching Match => m_Match;

        private Sprite m_CardFace;
        private Sprite m_CardBack;

        private QuestMatching m_Matching;
        private QuestUtilLabelMatching m_Match;

        private bool m_IsComplete;

        protected override void Start()
        {
            m_Matching = GetComponentInParent<QuestMatching>();

            if(m_Matching)
                m_Quest = m_Matching;
            else
                Debug.LogError("QuestMatching Component not found");   

            if(Image)
                m_CardBack = Image.sprite;
            else
                Debug.LogError("Image Component not found");            
        }

        public virtual void SetMatch(QuestUtilLabelMatching match)
        {
            this.m_Match = match;

            m_CardFace = match.Image;
        }

        public override void OnClick()
        {
            Image.sprite = m_CardFace;

            m_Matching.Open(this);
        }

        public virtual void Close()
        {
            if(!m_IsComplete)
            {
                if(m_CardBack)
                    Image.sprite = m_CardBack;
                Button.interactable = true;
            }
        }

        public override void Disable()
        {
            Button.interactable = false;
        }

        public virtual void Complete()
        {
            m_IsComplete = true;

            Button.interactable = false;

            Invoke("Hide", m_Matching.PeekDuration);
        }

        public virtual void Hide()
        {
            Image.enabled = false;
        }

        public override void Reset()
        {
            m_IsComplete = false;

            Button.interactable = true;

            Close();
        }
    }
}