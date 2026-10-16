using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMatchingCard : QuestComponent
    {
        private const string c_HideMethod = "Hide";

        public QuestUtilLabel Match => m_Match;

        private Sprite m_CardFace;
        private Sprite m_CardBack;

        private QuestMatching m_Matching;
        private QuestUtilLabel m_Match;

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

        public virtual void SetMatch(QuestUtilLabel match)
        {
            this.m_Match = match;

            m_CardFace = match.Image;
        }

        public override void OnClick()
        {
            Image.sprite = m_CardFace;

            m_Matching.Open(this);
        }

        public void Close()
        {
            CloseInternal();
        }

        protected virtual void CloseInternal()
        {
            if(!m_IsComplete)
            {
                if(m_CardBack)
                    Image.sprite = m_CardBack;
                Button.interactable = true;
            }
        }

        protected override void DisableInternal()
        {
            Button.interactable = false;
        }

        public virtual void Complete()
        {
            m_IsComplete = true;

            Button.interactable = false;

            Invoke(c_HideMethod, m_Matching.PeekDuration);
        }

        public virtual void Hide()
        {
            Image.enabled = false;
        }

        protected override void ResetInternal()
        {
            m_IsComplete = false;

            Button.interactable = true;

            CloseInternal();
        }
    }
}