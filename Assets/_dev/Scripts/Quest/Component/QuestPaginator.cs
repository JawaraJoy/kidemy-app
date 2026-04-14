using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestPaginator : MonoBehaviour
    {
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageIndicator;

        private int totalPages;
        private int currentPageIndex = 0;
        

        void Start()
        {
            totalPages = transform.childCount;

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

            if(previousButton)
                previousButton.interactable = false;
        }

        public void Next()
        {
            int tNextPage = currentPageIndex + 1;

            if(tNextPage < totalPages)
                ChangePage(tNextPage);

            if(nextButton)
            {
                if(tNextPage + 1 >= totalPages)
                    nextButton.interactable = false;
                else
                    nextButton.interactable = true;
            }

            if(previousButton)
                previousButton.interactable = true;
        }

        public void Previous()
        {
             int tNextPage = currentPageIndex - 1;

            if(tNextPage >= 0)
                ChangePage(tNextPage);

            if(previousButton)
            {
                if(tNextPage - 1 < 0)
                    previousButton.interactable = false;
                else
                    previousButton.interactable = true;
            }

            if(nextButton)
                nextButton.interactable = true;
        }

        public void ChangePage(int index)
        {
            int i = 0;

            foreach (Transform child in transform)
            {
                if(i == index)
                {
                    currentPageIndex = i;
                    child.gameObject.SetActive(true);
                }
                else
                    child.gameObject.SetActive(false);
                
                i++;
            }

            if(pageIndicator)
                pageIndicator.text = "Page " + (currentPageIndex + 1) + " of " + totalPages;
        }
    }
}