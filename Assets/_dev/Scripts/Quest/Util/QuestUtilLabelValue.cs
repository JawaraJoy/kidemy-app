using System;
using UnityEngine;

namespace EduGame
{
    [Serializable]
    public class QuestUtilLabelValue : QuestUtilLabel
    {
        [SerializeField] private string value;

        public string Value => value;
    }

}
