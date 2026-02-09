using System;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    [Serializable]
    public class QuestUtilLabelMatching : QuestUtilLabel
    {
        [SerializeField] private Color color;

        public Color Color => color;
        public int Id { get; private set; }

        public void SetId(int id)
        {
            Id = id;
        }
    }

}
