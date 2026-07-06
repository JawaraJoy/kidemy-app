using UnityEngine;

namespace EduGame
{
    public class QuestColoringCursor : MonoBehaviour
    {
        [Header("Custom Cursor Settings")]
        [SerializeField] private Texture2D baseCursorTexture;
        
        [Range(0f, 1f)]
        [Tooltip("How close to pure white a pixel must be to get tinted.")]
        [SerializeField] private float whiteThreshold = 0.9f;

        private bool isCustomCursorActive = false;
        private Texture2D tintedCursorTexture;

        public void ToggleCursor(Color tintColor)
        {
            if (isCustomCursorActive) ResetToDefault();
            else SetToCustom(tintColor);
        }

        public void SetToCustom(Color tintColor)
        {
            if (baseCursorTexture == null) return;

            ClearTintedTexture();

            tintedCursorTexture = new Texture2D(baseCursorTexture.width, baseCursorTexture.height, TextureFormat.RGBA32, false);
            Color[] pixels = baseCursorTexture.GetPixels();

            Vector2 hotSpot = new Vector2(0, baseCursorTexture.height);

            for (int i = 0; i < pixels.Length; i++)
            {
                Color originalPixel = pixels[i];

                // Check if the pixel is bright enough to be considered "white"
                if (originalPixel.r >= whiteThreshold &&
                    originalPixel.g >= whiteThreshold &&
                    originalPixel.b >= whiteThreshold)
                {
                    // Tint only the white area, keeping the asset's original transparency
                    pixels[i].r = tintColor.r * originalPixel.r;
                    pixels[i].g = tintColor.g * originalPixel.g;
                    pixels[i].b = tintColor.b * originalPixel.b;
                }
                // Black lines, gray shading, or outer borders are completely skipped and left alone
            }

            tintedCursorTexture.SetPixels(pixels);
            tintedCursorTexture.Apply();

            Cursor.SetCursor(tintedCursorTexture, hotSpot, CursorMode.Auto);
            isCustomCursorActive = true;
        }

        public void ResetToDefault()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            isCustomCursorActive = false;
            ClearTintedTexture();
        }

        private void ClearTintedTexture()
        {
            if (tintedCursorTexture != null)
            {
                Destroy(tintedCursorTexture);
                tintedCursorTexture = null;
            }
        }

        private void OnDisable()
        {
            ResetToDefault();
        }
    }
}