using UnityEngine;

namespace Rush
{
    [CreateAssetMenu(fileName = "Key_", menuName = "Rush/Game/Key")]
    public class QuestKeyConfig : Config
    {
        [SerializeField]
        protected int m_KeyNumber = 0;
        [SerializeField]
        protected UISettingField m_UISetting;
        public int KeyNumber => m_KeyNumber;


        public void Config(Extenable extenable)
        {
            m_UISetting.Config(extenable);
        }
    }
}
