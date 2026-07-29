using UnityEngine;

namespace Rush
{
    public class PanelView : UIView
    {
        [SerializeField]
        private PanelConfig m_PanelConfig;
        public PanelConfig PanelConfig => m_PanelConfig;
    }
}
