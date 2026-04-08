using System;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    [Serializable]
    public class QuestUtilLabelGroup
    {
        [SerializeField] private QuestUtilLabel label;
        [SerializeField] private int limit = -1;
        [SerializeField] private QuestUtilLabel[] labels;

        public int Id { get; set; }
        public QuestUtilLabel Label => label;
        public int Limit => limit;
        public QuestUtilLabel[] Labels => labels;
        
    }

}
