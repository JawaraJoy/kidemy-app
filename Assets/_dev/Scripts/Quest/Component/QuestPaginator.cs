using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestPaginator : MonoBehaviour
    {
        private const string c_PagePrefix = "Page ";
        private const string c_PageOf = " of ";

        [SerializeField] [FormerlySerializedAs("previousButton")] private Button m_PreviousButton;
        [SerializeField] [FormerlySerializedAs("nextButton")] private Button m_NextButton;
        [SerializeField] [FormerlySerializedAs("pageIndicator")] private TMP_Text m_PageIndicator;

        private int m_TotalPages;
        private int m_CurrentPageIndex = 0;
        

        void Start()
        {
            m_TotalPages = transform.childCount;

            bool firstPage = true;

            foreach (Transform child in transform)
            {
                if(firstPage)
                {
                    child.gameObject.SetActive(true);
                    firstPage = false;
                }
                else
                    child.gameObject.SetActive(false);
            }

            if(m_PreviousButton)
                m_PreviousButton.interactable = false;
        }

        public void Next()
        {
            int tNextPage = m_CurrentPageIndex + 1;

            if(tNextPage < m_TotalPages)
                ChangePage(tNextPage);

            if(m_NextButton)
            {
                if(tNextPage + 1 >= m_TotalPages)
                    m_NextButton.interactable = false;
                else
                    m_NextButton.interactable = true;
            }

            if(m_PreviousButton)
                m_PreviousButton.interactable = true;
        }

        public void Previous()
        {
             int tNextPage = m_CurrentPageIndex - 1;

            if(tNextPage >= 0)
                ChangePage(tNextPage);

            if(m_PreviousButton)
            {
                if(tNextPage - 1 < 0)
                    m_PreviousButton.interactable = false;
                else
                    m_PreviousButton.interactable = true;
            }

            if(m_NextButton)
                m_NextButton.interactable = true;
        }

        private void ChangePage(int index)
        {
            int i = 0;

            foreach (Transform child in transform)
            {
                if(i == index)
                {
                    m_CurrentPageIndex = i;
                    child.gameObject.SetActive(true);
                }
                else
                    child.gameObject.SetActive(false);
                
                i++;
            }

            if(m_PageIndicator)
                m_PageIndicator.text = c_PagePrefix + (m_CurrentPageIndex + 1) + c_PageOf + m_TotalPages;
        }
    }
}