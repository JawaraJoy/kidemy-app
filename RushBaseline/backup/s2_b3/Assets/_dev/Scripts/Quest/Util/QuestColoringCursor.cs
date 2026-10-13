using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestColoringCursor : MonoBehaviour
    {
        [Header("Custom Cursor Settings")]
        [SerializeField] [FormerlySerializedAs("baseCursorTexture")] private Texture2D m_BaseCursorTexture;
        
        [Range(0f, 1f)]
        [Tooltip("How close to pure white a pixel must be to get tinted.")]
        [SerializeField] [FormerlySerializedAs("whiteThreshold")] private float m_WhiteThreshold = 0.9f;

        private bool m_IsCustomCursorActive = false;
        private Texture2D m_TintedCursorTexture;

        public void ToggleCursor(Color tintColor)
        {
            if (m_IsCustomCursorActive) ResetToDefault();
            else SetToCustom(tintColor);
        }

        public void SetToCustom(Color tintColor)
        {
            if (m_BaseCursorTexture == null) return;

            ClearTintedTexture();

            m_TintedCursorTexture = new Texture2D(m_BaseCursorTexture.width, m_BaseCursorTexture.height, TextureFormat.RGBA32, false);
            Color[] pixels = m_BaseCursorTexture.GetPixels();

            Vector2 hotSpot = new Vector2(0, m_BaseCursorTexture.height);

            for (int i = 0; i < pixels.Length; i++)
            {
                Color originalPixel = pixels[i];

                // Check if the pixel is bright enough to be considered "white"
                if (originalPixel.r >= m_WhiteThreshold &&
                    originalPixel.g >= m_WhiteThreshold &&
                    originalPixel.b >= m_WhiteThreshold)
                {
                    // Tint only the white area, keeping the asset's original transparency
                    pixels[i].r = tintColor.r * originalPixel.r;
                    pixels[i].g = tintColor.g * originalPixel.g;
                    pixels[i].b = tintColor.b * originalPixel.b;
                }
                // Black lines, gray shading, or outer borders are completely skipped and left alone
            }

            m_TintedCursorTexture.SetPixels(pixels);
            m_TintedCursorTexture.Apply();

            Cursor.SetCursor(m_TintedCursorTexture, hotSpot, CursorMode.Auto);
            m_IsCustomCursorActive = true;
        }

        public void ResetToDefault()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            m_IsCustomCursorActive = false;
            ClearTintedTexture();
        }

        private void ClearTintedTexture()
        {
            if (m_TintedCursorTexture != null)
            {
                Destroy(m_TintedCursorTexture);
                m_TintedCursorTexture = null;
            }
        }

        private void OnDisable()
        {
            ResetToDefault();
        }
    }
}