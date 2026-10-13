using System;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    [Serializable]
    public class QuestUtilLabelMatching : QuestUtilLabel
    {
        public void SetId(int id)
        {
            Id = id;
        }
    }

}
