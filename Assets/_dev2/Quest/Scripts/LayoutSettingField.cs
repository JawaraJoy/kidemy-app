using UnityEngine;

namespace EduGame
{
    [System.Serializable]
    public class LayoutSettingField 
    {
        [SerializeField]
        private bool m_UseLayoutSetting = false;
        // you can add another layout setting here, like padding, spacing, etc.
        // it's target layoutelement component, so you can add more properties if needed
        [SerializeField]
        private float m_FleksibleWidth = 1f;
        [SerializeField]
        private float m_FleksibleHeight = 1f;
        public bool UseLayoutSetting => m_UseLayoutSetting;
        public float FleksibleWidth => m_FleksibleWidth;
        public float FleksibleHeight => m_FleksibleHeight;
    }
}
