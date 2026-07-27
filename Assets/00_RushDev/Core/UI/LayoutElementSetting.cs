using UnityEngine;
using UnityEngine.UI;

namespace Rush
{
    [System.Serializable]
    public class LayoutElementSetting
    {
        [SerializeField]
        private bool m_IgnoreLayout = false;
        [SerializeField]
        private float m_FlexibleWidht = 0f;
        [SerializeField]
        private float m_FlexibleHight = 0f;

        public void Config(GameObject compo)
        {
            if (compo.TryGetComponent(out LayoutElement layoutElement))
            {
                layoutElement.ignoreLayout = m_IgnoreLayout;
                layoutElement.flexibleWidth = m_FlexibleWidht;
                layoutElement.flexibleHeight = m_FlexibleHight;
            }
        }
    }
}
