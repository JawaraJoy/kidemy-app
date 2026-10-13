using System;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    [Serializable]
    public class QuestUtilButton : QuestUtilGameObject
    {
        private Button m_Button;
        public Button Button => m_Button;

        protected override void Awake()
        {
            base.Awake();

            m_Button = GetComponent<Button>();
        }

        protected override void Enable()
        {
            Button.interactable = true;
        }

        public override void Disable()
        {
            Button.interactable = false;
        }
    }
}
