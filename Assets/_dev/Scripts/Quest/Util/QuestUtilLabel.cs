using System;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    [Serializable]
    public class QuestUtilLabel
    {
        [SerializeField] private Sprite image;
        [SerializeField] private string text;

        public Sprite Image => image;
        public string Text => text;
    }

}
