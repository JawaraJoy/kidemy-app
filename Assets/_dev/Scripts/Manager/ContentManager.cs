using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

namespace EduGame
{
    public class ContentManager : MonoBehaviour
    {
        private CanvasScaler canvasScaler;

        // Import the native JS functions
        [DllImport("__Internal__")]
        private static extern int GetBrowserWidth();

        [DllImport("__Internal__")]
        private static extern int GetBrowserHeight();

        private Vector2 refRes = Vector2.one;
        private Vector2 currentViewportSize = Vector2.zero;

        void Awake()
        {
            canvasScaler = GetComponent<CanvasScaler>();

            refRes = canvasScaler.referenceResolution + Vector2.up * 200;

            currentViewportSize = refRes;

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
            Debug.Log($"Ref Viewport: {refRes.x}x{ refRes.y}");     

            if (viewportSize != currentViewportSize)
            {
                currentViewportSize = viewportSize;

                // Calculate the aspect ratios
                float deviceAspect = viewportSize.x / viewportSize.y;
                float referenceAspect = refRes.x / refRes.y;

                canvasScaler.matchWidthOrHeight = deviceAspect > referenceAspect ? 1f : 0f;
            }
        }
    }
}