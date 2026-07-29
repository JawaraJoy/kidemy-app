using UnityEngine;

namespace Rush
{
    [CreateAssetMenu(fileName = "UISetting_", menuName = "Rush/UI/Setting")]
    public class UISettingConfig : Config
    {
        [SerializeField]
        private UISettingField m_UISettingField;

        public void Config(UIView uIView)
        {
            m_UISettingField.Config(uIView);
        }
    }
}
