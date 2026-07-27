using UnityEngine;

namespace Rush
{
    public abstract class PanelView : View
    {
        [SerializeField]
        private PanelConfig m_PanelConfig;
        public PanelConfig PanelConfig => m_PanelConfig;
    }
}
