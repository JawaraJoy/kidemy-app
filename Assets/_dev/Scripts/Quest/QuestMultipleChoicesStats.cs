using EasyTextEffects;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMultipleChoicesStats : QuestMultipleChoices
    {
        [SerializeField] private RectTransform[] statPanels;

        private SO_QuestMultipleChoiceStats dataMultipleChoiceStats;

        protected override void Start()
        {
            if(!dataMultipleChoiceStats)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestMultipleChoiceStats");
            
            for (int i = 0; i < statPanels.Length; i++)
            {
                if (i < dataMultipleChoiceStats.Stats.Length)
                {
                    bool isNumber = false;
                    int index = StringHelper.ToInt(dataMultipleChoiceStats.Stats[i].Text, out isNumber);

                    if (isNumber)
                    {
                        statPanels[i].gameObject.SetActive(true);
                        QuestUtilLabel stat = dataMultipleChoiceStats.Stats[i];

                        for (int j = 0; j < index; j++)
                        {
                            GameObject unit = new GameObject("Unit");
                            unit.transform.SetParent(statPanels[i]);
                            unit.transform.localPosition = Vector3.zero;
                            unit.transform.localScale = Vector3.one;
                            unit.AddComponent<Image>().sprite = stat.Image;  
                        }
                    }
                    else
                    {
                        statPanels[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    statPanels[i].gameObject.SetActive(false);
                }
            }

            base.Start();
        }

        public override void Setup()
        {
            dataMultipleChoiceStats = data as SO_QuestMultipleChoiceStats;

            base.Setup();
        }

        public override void ShowTutorial()
        {
            //GameManager.Instance.ShowTutorial(correctChoice.Rect);
        }
    }
}