using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    [Serializable]
    public class QuestUtilLabelGroup
    {
        [SerializeField] [FormerlySerializedAs("label")] private QuestUtilLabel m_Label;
        [SerializeField] [FormerlySerializedAs("limit")] private int m_Limit = -1;
        [SerializeField] [FormerlySerializedAs("labels")] private QuestUtilLabel[] m_Labels;

        public int Id { get; set; }
        public QuestUtilLabel Label => m_Label;
        public int Limit => m_Limit;
        public QuestUtilLabel[] Labels => m_Labels;
        
    }

}
