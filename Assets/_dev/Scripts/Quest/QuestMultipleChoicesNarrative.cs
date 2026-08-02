using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMultipleChoicesNarrative : QuestMultipleChoices
    {
        [Header("Data")]
        
        [Header("Components")]
        [SerializeField] private TMP_Text narrativeText;
        
        [Header("Prefab")]
        [SerializeField] private QuestMultipleChoicesItem choiceItemPrefab;

        private SO_QuestMultipleChoiceNarrative dataMultipleChoiceNarrative;
        
        protected override void Start()
        {
            base.Start();
            
            if(!dataMultipleChoiceNarrative)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestMultipleChoiceNarrative");

            if(narrativeText)
                narrativeText.text = dataMultipleChoiceNarrative.Narrative;
        }

        public override void Setup()
        {
            dataMultipleChoiceNarrative = data as SO_QuestMultipleChoiceNarrative;

            base.Setup();
        }
    }
}