using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class ChallengeManager : MonoBehaviour
    {
        [SerializeField] private SO_Challenge data;

        [Header("Result Page")]
        [SerializeField] private RectTransform result;
        [SerializeField] private TMP_Text score;
        [SerializeField] private RectTransform failedResult;
        [SerializeField] private RectTransform goodResult;
        [SerializeField] private RectTransform perfectResult;

        private List<Quest> initiatedQuests = new List<Quest>();
        private int currentIndex = 0;
        private int accumulatedScore = 0;

        public int CurrentQuestNumber => currentIndex + 1;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (!data || data.Quests.Length == 0)
                Debug.LogError("Data is not set, challenge will return empty");

            foreach (var quest in data.Quests)
            {
                if (quest.Active)
                {
                    if (!quest.Quest)
                    {
                        Debug.LogError("Quest data is not set, quest will be skipped");
                        continue;
                    }

                    if (!quest.Template)
                    {
                        Debug.LogError("Template is not set, quest will be skipped");
                        continue;
                    }

                    Quest template = InstantiateTemplate(quest.Template);
                    template.Init(quest.Quest);
                    template.gameObject.SetActive(false);

                    initiatedQuests.Add(template);
                }
            }

            if(result)
                result.gameObject.SetActive(false);
            
            if(failedResult)
                failedResult.gameObject.SetActive(false);
            
            if(goodResult)
                goodResult.gameObject.SetActive(false);
            
            if(perfectResult)
                perfectResult.gameObject.SetActive(false);

            SetPage(0);
        }

        Quest InstantiateTemplate(Quest templatePrefab)
        {
            Quest template = Instantiate(templatePrefab, transform);

            template.transform.localPosition = Vector3.zero;
            template.transform.localScale = Vector3.one;

            return template;
        }

        public void Previous()
        {
            SetPage(currentIndex, false);

            currentIndex--;

            SetPage(currentIndex, true);
        }

        public void Next()
        {
            SetPage(currentIndex, false);

            currentIndex++;

            if (!SetPage(currentIndex, true))
                Finish();
        }

        public bool SetPage(int index, bool status = true)
        {
            bool res = false;

            if (initiatedQuests.Count > index && index >= 0)
            {
                if(status)
                    initiatedQuests[index].Enabled();
                else
                    initiatedQuests[index].Disabled();

                res = true;
            }

            return res;
        }

        public void Finish()
        {
            if(result)
                result.gameObject.SetActive(true);

            if(score)
                score.text = score.text + " " + accumulatedScore;
            
            if(accumulatedScore < 80 && failedResult)
                failedResult.gameObject.SetActive(true);
            else if(accumulatedScore >= 80 && accumulatedScore < 90 && goodResult)
                goodResult.gameObject.SetActive(true);
            else if(accumulatedScore == 120 && perfectResult)
                perfectResult.gameObject.SetActive(true);
        }

        public void AddScore(int score)
        {
            accumulatedScore += score;
        }
    }
}