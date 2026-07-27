using MoreMountains.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Rush
{
    public class CanvasManager : Singleton<CanvasManager>
    {
        [SerializeField, MMReadOnly]
        private List<CanvasView> m_CanvasViews = new();
        public List<CanvasView> CanvasViews => m_CanvasViews;

        [SerializeField, MMReadOnly]
        private List<PanelView> m_ShowedPanels = new List<PanelView>();

        [SerializeField]
        private UnityEvent<PanelView> m_OnAnyBusyPanelShow;
        [SerializeField]
        private UnityEvent<PanelView> m_OnNoBusyPanelShow;
        public UnityEvent<PanelView> OnAnyBusyPanelShow => m_OnAnyBusyPanelShow;
        public UnityEvent<PanelView> OnNoBusyPanelShow => m_OnNoBusyPanelShow;

        // invoke this manualy in context menu, after all canvas and panel are set on inspector.
        [ContextMenu("Refresh")] 
        private void Configurate()
        {
            foreach (var canvasView in m_CanvasViews)
            {
                canvasView.Configurate();
            }
        }
        private CanvasView GetCanvasViewInternal(CanvasConfig canvasConfig)
        {
            foreach (var canvasView in m_CanvasViews)
            {
                if (canvasView.CanvasConfig.Info.Id == canvasConfig.Info.Id)
                {
                    return canvasView;
                }
            }
            return null;
        }   
        protected bool HasCanvasInternal(CanvasConfig canvasConfig, out CanvasView canvas)
        {
            canvas = GetCanvasViewInternal(canvasConfig);
            return canvas != null;
        }
        public bool HasCanvas(CanvasConfig canvasConfig, out CanvasView canvas)
        {
            return HasCanvasInternal(canvasConfig, out canvas);
        }
        public void RegisterCanvasView(CanvasView canvasView)
        {
            if (HasCanvasInternal(canvasView.CanvasConfig, out CanvasView existed))
            {
                
            }
            else
            {
                m_CanvasViews.Add(canvasView);
            }
        }
        public void UnRegisterCanvasView(CanvasView canvasView)
        {
            if (HasCanvasInternal(canvasView.CanvasConfig, out CanvasView existed))
            {
                m_CanvasViews.Remove(canvasView);
            }
            else
            {
                
            }
        }
        public void ShowPanel(PanelConfig panelConfig)
        {
            if (panelConfig == null)
            {
                return;
            }
            if (HasCanvasInternal(panelConfig.AttachedCanvas, out var canvas))
            {
                if (canvas.HasPanel(panelConfig, out var panel))
                {
                    panel.Show();
                    m_ShowedPanels.Add(panel);
                    OnAnyBusyPanelShowed(panel);
                }
            }
        }
        public void HidePanel(PanelConfig panelConfig)
        {
            if (panelConfig == null)
            {
                return;
            }
            if (HasCanvasInternal(panelConfig.AttachedCanvas, out var canvas))
            {
                if (canvas.HasPanel(panelConfig, out var panel))
                {
                    panel.Hide();
                    m_ShowedPanels.Remove(panel);
                    OnNoBusyPanelShowed(panel);
                }
            }
        }
        public T GetPanel<T>() where T : PanelView
        {
            foreach (var canvasView in m_CanvasViews)
            {
                if (canvasView.HasPanel(out T panel))
                {
                    return panel;
                }
            }
            return null;
        }

        protected void OnAnyBusyPanelShowed(PanelView panel)
        {
            if (m_ShowedPanels.Count > 0)
            {
                m_OnAnyBusyPanelShow.Invoke(panel);
            }
        }
        protected void OnNoBusyPanelShowed(PanelView panel)
        {
            if (m_ShowedPanels.Count == 0)
            {
                m_OnNoBusyPanelShow.Invoke(panel);
            }
        }
    }
}
