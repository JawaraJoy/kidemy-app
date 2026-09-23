using EasyTextEffects;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMultipleChoicesAudio : QuestMultipleChoices
    {
        [SerializeField] protected AudioPlayer audioPlayer;

        public override void ShowTutorial()
        {
            GameManager.Instance.ShowTutorialCustom(
                new[]
                    {
                        audioPlayer.transform.GetComponent<RectTransform>(),
                        correctChoice.transform.GetComponent<RectTransform>()
                    }
            );
        }
    }
}