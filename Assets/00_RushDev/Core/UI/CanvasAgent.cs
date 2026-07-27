using UnityEngine;

namespace Rush
{
    // A component for drag n drop on event for CanvasManager
    public class CanvasAgent : MonoBehaviour
    {
        public void ShowPanel(PanelConfig config)
        {
            CanvasManager.Instance.ShowPanel(config);
        }
        public void HidePanel(PanelConfig config)
        {
            CanvasManager.Instance.HidePanel(config);
        }
    }
}
