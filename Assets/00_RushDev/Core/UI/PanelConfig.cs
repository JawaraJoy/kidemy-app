using MoreMountains.Tools;
using UnityEngine;

namespace Rush
{
    [CreateAssetMenu(fileName = "Panel_", menuName = "Rush/UI/Panel")]
    public class PanelConfig : Config
    {
        [SerializeField]
        private bool m_IsBusyPanel;
        [SerializeField, MMReadOnly]
        private CanvasConfig m_AttachedCanvas;
        public bool IsBusyPanel => m_IsBusyPanel;
        public CanvasConfig AttachedCanvas => m_AttachedCanvas;

        public void AttachToCanvas(CanvasView canvasView)
        {
            m_AttachedCanvas = canvasView.CanvasConfig;
        }

        public void Show()
        {
            CanvasManager.Instance.ShowPanel(this);
        }
        public void Hide()
        {
            CanvasManager.Instance.HidePanel(this);
        }
    }
}
