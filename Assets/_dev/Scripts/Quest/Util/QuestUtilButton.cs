using System;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    [Serializable]
    public class QuestUtilButton : QuestUtilGameObject
    {
        public Button Button { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            Button = GetComponent<Button>();
        }

        public override void Enable()
        {
            Button.interactable = true;
        }

        public override void Disable()
        {
            Button.interactable = false;
        }
    }
}
