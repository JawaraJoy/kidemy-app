using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

namespace EduGame
{
    public class ContentManager : MonoBehaviour
    {
        private const int c_RefResExtraHeight = 200;

        private CanvasScaler m_CanvasScaler;

        // Import the native JS functions
        [DllImport("__Internal__")]
        private static extern int GetBrowserWidth();

        [DllImport("__Internal__")]
        private static extern int GetBrowserHeight();

        private Vector2 m_RefRes = Vector2.one;
        private Vector2 m_CurrentViewportSize = Vector2.zero;

        void Awake()
        {
            TryGetComponent<CanvasScaler>(out m_CanvasScaler);

            m_RefRes = m_CanvasScaler.referenceResolution + Vector2.up * c_RefResExtraHeight;

            m_CurrentViewportSize = m_RefRes;

            // Get the Canvas Scaler component
            UpdateScreen();
        }

        void Update()
        {
            UpdateScreen();
        }

        void UpdateScreen()
        {
            // Get the reference resolution (Vector2)

            Vector2 viewportSize = Vector2.zero;

#if UNITY_WEBGL && !UNITY_EDITOR
            viewportSize.x = GetBrowserWidth();
            viewportSize.y = GetBrowserHeight();

            Debug.Log($"Browser Viewport: {viewportSize.x}x{ viewportSize.y}");
#else
            // Fallback for running inside the Unity Editor
            viewportSize.x = Screen.width;
            viewportSize.y = Screen.height;
#endif

            Debug.Log($"Browser Viewport: {viewportSize.x}x{ viewportSize.y}");     
            Debug.Log($"Ref Viewport: {m_RefRes.x}x{ m_RefRes.y}");     

            if (viewportSize != m_CurrentViewportSize)
            {
                m_CurrentViewportSize = viewportSize;

                // Calculate the aspect ratios
                float deviceAspect = viewportSize.x / viewportSize.y;
                float referenceAspect = m_RefRes.x / m_RefRes.y;

                m_CanvasScaler.matchWidthOrHeight = deviceAspect > referenceAspect ? 1f : 0f;
            }
        }
    }
}