using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    [Serializable]
    public class QuestUtilLabelChoice : QuestUtilLabel
    {
        [SerializeField] [FormerlySerializedAs("isAnswer")] private bool m_IsAnswer;

        public bool IsAnswer => m_IsAnswer;
    }

}
