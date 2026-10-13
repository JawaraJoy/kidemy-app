using System;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    [Serializable]
    public class QuestUtilLabelChoice : QuestUtilLabel
    {
        [SerializeField] private bool isAnswer;

        public bool IsAnswer => isAnswer;
    }

}
