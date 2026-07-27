using UnityEngine;

namespace Rush
{
    public class CanvasView : View
    {
        [SerializeField]
        private CanvasConfig m_CanvasConfig;
        [SerializeField]
        private Canvas m_Canvas;
        [SerializeField]
        private PanelView[] m_PanelViews;
        public Canvas Canvas => m_Canvas;
        public CanvasConfig CanvasConfig => m_CanvasConfig;
        public PanelView[] PanelViews => m_PanelViews;

        private void Start()
        {
            CanvasManager.Instance.RegisterCanvasView(this);

            if (m_Canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                m_Canvas.worldCamera = Camera.main;
            }
            
        }
        public void Configurate()
        {
            foreach (var panelView in m_PanelViews)
            {
                panelView.PanelConfig.AttachToCanvas(this);
            }
        }

        protected PanelView GetPanelInternal(PanelConfig panelConfig)
        {
            foreach (var panelView in m_PanelViews)
            {
                if (panelView.PanelConfig.Info.Id == panelConfig.Info.Id)
                {
                    return panelView;
                }
            }
            return null;
        }
        protected T GetPanelInternal<T>() where T : PanelView
        {
            foreach (var panelView in m_PanelViews)
            {
                if (panelView is T tPanel)
                {
                    return tPanel;
                }
            }
            return null;
        }
        protected bool HasPanelInternal<T>(out T panel) where T : PanelView
        {
            panel = GetPanelInternal<T>();
            return panel != null;

        }
        public bool HasPanel<T>(out T panel) where T : PanelView
        {
            return HasPanelInternal(out panel);
        }
        protected bool HasPanelInternal(PanelConfig panelConfig, out PanelView panel) 
        {
            panel = GetPanelInternal(panelConfig);
            return panel != null;
        }
        public bool HasPanel(PanelConfig panelConfig, out PanelView panel)
        {
            if (panelConfig == null)
            {
                panel = null;
                return false;
            }
            return HasPanelInternal(panelConfig, out panel);
        }
    }
}
