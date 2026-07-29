using UnityEngine;

namespace Rush
{
    [System.Serializable]
    public class UISettingField
    {
        [SerializeField]
        private LayoutElementSetting m_LayoutElement;
        [SerializeField]
        private LayoutGroupSetting m_LayoutGroup;
        public void Config(Extenable extenable)
        {
            m_LayoutElement.Config(extenable);
            m_LayoutGroup.Config(extenable);
        }
    }
}
