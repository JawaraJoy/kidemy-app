using UnityEngine;

namespace EduGame
{
    [System.Serializable]
    public class ResultContext
    {
        private readonly int m_WrongCount;
        private readonly int m_RightCount;
        public int WrongCount => m_WrongCount;
        public int RightCount => m_RightCount;
        public ResultContext(int wrongCount, int rightCount)
        {
            m_WrongCount = wrongCount;
            m_RightCount = rightCount;
        }

        public float GetRate()
        {
            int totalQuest = m_WrongCount + m_RightCount;
            float rate = (float)m_RightCount / (float)totalQuest;
            return rate;
        }
        public int GetTotalQuest()
        {
            return m_WrongCount + m_RightCount;
        }
    }
}
