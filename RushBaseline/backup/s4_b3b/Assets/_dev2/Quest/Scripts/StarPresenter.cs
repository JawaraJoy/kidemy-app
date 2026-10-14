using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace EduGame
{
    public class StarPresenter : MonoBehaviour
    {
        [System.Serializable]
        public class Star
        {
            [SerializeField]
            private GameObject m_StarContent;
            [SerializeField]
            private GameObject m_ActiveStarContent;
            [SerializeField]
            private UnityEvent m_OnStarContentShow;
            [SerializeField]
            private UnityEvent m_OnActiveStarContentShow;

            public void SetStarContent(bool active)
            {
                m_StarContent.SetActive(active);
                if (active)
                {
                    m_OnStarContentShow.Invoke();
                }
            }
            public void SetActiveStar(bool active)
            {
                m_ActiveStarContent.SetActive(active);
                if (active)
                {
                    m_OnActiveStarContentShow.Invoke();
                }
            }
        }
        [SerializeField]
        private Star[] m_Stars;

        private const float c_PerfectThresholdRate = 1.0f;
        private const float c_MiddleThresholdRate = 0.5f;
        private const float c_ZeroThresholdRate = 0f;

        private int m_StarCount;
        private ResultPanel m_ResultPanel;
        public void SetResultPanel(ResultPanel resultPanel)
        {
            m_ResultPanel = resultPanel;
        }

        public void DisableAllStars()
        {
            foreach (var item in m_Stars)
            {
                item.SetStarContent(false);
                item.SetActiveStar(false);
            }
        }
        public void StarsResult(ResultContext resultContext)
        {
            m_StarCount = 0;

            float rate = resultContext.GetRate();
            string reactionWord = "Oh nooo";
            if (rate > c_ZeroThresholdRate)
            {
                m_StarCount++;
                reactionWord = "Good Job!";
            }
            if (rate >= c_MiddleThresholdRate)
            {
                m_StarCount++;
                reactionWord = "Excellent!!";
            }
            if (rate >= c_PerfectThresholdRate)
            {
                m_StarCount++;
                reactionWord = "Smart Kid!!!";
            }
            StartCoroutine(SetActiveStars(true));

            m_ResultPanel.SetLabelText(reactionWord);
        }

        private IEnumerator SetActiveStars(bool active)
        {
            yield return new WaitForSeconds(1f);
            for (int i = 0; i < m_Stars.Length; i++)
            {
                m_Stars[i].SetStarContent(active);
                yield return new WaitForSeconds(0.2f);
                if (i < m_StarCount)
                {
                    m_Stars[i].SetActiveStar(active);
                }
                
            }
            GameManager.Instance.SetRightAsnwer(0);
        }
    }
}
