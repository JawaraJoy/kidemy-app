using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EduGame
{
    [RequireComponent(typeof(QuestUtilRegionalDrag))]

    public class QuestColoringField : MonoBehaviour
    {
        private RectTransform m_Rect;
        private RenderTexture m_Rt;
        private CommandBuffer m_Cb;
        private QuestUtilRegionalDrag m_E;
        private QuestColoring m_QuestColoring;
        private Texture m_BaseTexture;

        public float Result => GetPaintedPercentage();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            TryGetComponent<RawImage>(out RawImage image);

            TryGetComponent<RectTransform>(out m_Rect);

            TryGetComponent<QuestUtilRegionalDrag>(out m_E);

            m_QuestColoring = GetComponentInParent<QuestColoring>();

            if (!m_Rect)
                Debug.LogError("RectTransform Component not found");

            if (!m_E)
                Debug.LogError("QuestUtilRegionalDrag Component not found");

            if (!image)
                Debug.LogError("Image Component not found");

            if(!m_QuestColoring)
                Debug.LogError("QuestColoring Component not found");

            if (image)
            {
                m_BaseTexture = image.texture;

                // Create a RenderTexture with the same dimensions and format as the Texture2D
                m_Rt = new RenderTexture(image.texture.width, image.texture.height, 0, RenderTextureFormat.ARGB32);

                m_Cb = new CommandBuffer { name = "UIPainter" };
                
                if (m_BaseTexture)
                {
                    // This puts your image (with its alpha) onto the canvas
                    Graphics.Blit(m_BaseTexture, m_Rt);
                }

                image.texture = m_Rt;
            }
            else
                Debug.LogError("Texture not found");
        }

        // Update is called once per frame
        void Update()
        {
            if (m_E.Dragged)
                Paint(m_E.PointerData);
        }

        private void Paint(PointerEventData eventData)
        {
            // 3. Convert Screen Position (mouse) to Local UV coordinates (0 to 1)
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Rect, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                // RectTransform coordinates are based on pivot. We convert to 0-1 range.
                Vector2 normalizedUV = new Vector2(
                    (localPoint.x / m_Rect.rect.width) + 0.5f,
                    (localPoint.y / m_Rect.rect.height) + 0.5f
                );

                Draw(normalizedUV);
            }
        }

        void Draw(Vector2 uv)
        {
            m_QuestColoring.Brush.SetVector("_BrushPos", uv);

            RenderTexture tempRT = RenderTexture.GetTemporary(m_Rt.descriptor);

            m_Cb.Clear();
            m_Cb.Blit(m_Rt, tempRT, m_QuestColoring.Brush);
            m_Cb.Blit(tempRT, m_Rt);
            Graphics.ExecuteCommandBuffer(m_Cb);

            RenderTexture.ReleaseTemporary(tempRT);
        }

        public void Reset() 
        {
            // This instantly overwrites the colored RT with the clean original
            Graphics.Blit(m_BaseTexture, m_Rt);
        }

        private void OnDestroy()
        {
            m_Cb?.Release();
            if (m_Rt != null) m_Rt.Release();
        }

        protected float GetPaintedPercentage() {
            // 1. Create a tiny temporary RenderTexture
            // 64x64 is usually enough for a accurate score
            RenderTexture smallRT = RenderTexture.GetTemporary(64, 64);
            
            // 2. Downscale using Blit (GPU does the heavy lifting)
            // Use Bilinear filtering for better averaging of "painted" areas
            //rt.filterMode = FilterMode.Bilinear;
            Graphics.Blit(m_Rt, smallRT);

            // 3. Copy only the small texture to CPU
            Texture2D tex = new Texture2D(64, 64, TextureFormat.ARGB32, false);
            RenderTexture.active = smallRT;
            tex.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
            tex.Apply();

            // 4. Count colored pixels on the small array
            Color32[] pixels = tex.GetPixels32();

            int paintedCount = 0;
            int pixelCount = 0;

            for (int i = 0; i < pixels.Length; i++) {
                
                if(pixels[i].a > 20f)
                {
                    pixelCount++;

                    // If not white (using a small threshold for safety)
                    if (pixels[i].r < 250 || pixels[i].g < 250 || pixels[i].b < 250)
                        paintedCount++;
                }
            }

            // 5. Cleanup
            RenderTexture.ReleaseTemporary(smallRT);
            Destroy(tex);

            // Return percentage (0.0 to 1.0)
            return (float)paintedCount / pixelCount;
        }
    }
}