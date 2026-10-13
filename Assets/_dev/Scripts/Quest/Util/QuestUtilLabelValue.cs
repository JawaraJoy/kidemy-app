using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    [Serializable]
    public class QuestUtilLabelValue : QuestUtilLabel
    {
        [SerializeField] [FormerlySerializedAs("value")] private string m_Value;

        public string Value => m_Value;
    }

}
