using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EduGame
{
    [RequireComponent(typeof(QuestUtilRegionalDrag))]

    public class QuestColoringField : MonoBehaviour
    {
        private RectTransform rect;
        private RenderTexture rt;
        private CommandBuffer cb;
        private QuestUtilRegionalDrag e;
        private QuestColoring questColoring;
        private Texture baseTexture;

        public float Result => GetPaintedPercentage();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            RawImage image = GetComponent<RawImage>();

            rect = GetComponent<RectTransform>();

            e = GetComponent<QuestUtilRegionalDrag>();

            questColoring = GetComponentInParent<QuestColoring>();

            if (!rect)
                Debug.LogError("RectTransform Component not found");

            if (!e)
                Debug.LogError("QuestUtilRegionalDrag Component not found");

            if (!image)
                Debug.LogError("Image Component not found");

            if(!questColoring)
                Debug.LogError("QuestColoring Component not found");

            if (image)
            {
                baseTexture = image.texture;

                // Create a RenderTexture with the same dimensions and format as the Texture2D
                rt = new RenderTexture(image.texture.width, image.texture.height, 0, RenderTextureFormat.ARGB32);

                cb = new CommandBuffer { name = "UIPainter" };
                
                if (baseTexture)
                {
                    // This puts your image (with its alpha) onto the canvas
                    Graphics.Blit(baseTexture, rt);
                }

                image.texture = rt;
            }
            else
                Debug.LogError("Texture not found");
        }

        // Update is called once per frame
        void Update()
        {
            if (e.Dragged)
                Paint(e.PointerData);
        }

        private void Paint(PointerEventData eventData)
        {
            // 3. Convert Screen Position (mouse) to Local UV coordinates (0 to 1)
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                // RectTransform coordinates are based on pivot. We convert to 0-1 range.
                Vector2 normalizedUV = new Vector2(
                    (localPoint.x / rect.rect.width) + 0.5f,
                    (localPoint.y / rect.rect.height) + 0.5f
                );

                Draw(normalizedUV);
            }
        }

        void Draw(Vector2 uv)
        {
            questColoring.Brush.SetVector("_BrushPos", uv);

            RenderTexture tempRT = RenderTexture.GetTemporary(rt.descriptor);

            cb.Clear();
            cb.Blit(rt, tempRT, questColoring.Brush);
            cb.Blit(tempRT, rt);
            Graphics.ExecuteCommandBuffer(cb);

            RenderTexture.ReleaseTemporary(tempRT);
        }

        public void Reset() 
        {
            // This instantly overwrites the colored RT with the clean original
            Graphics.Blit(baseTexture, rt);
        }

        private void OnDestroy()
        {
            cb?.Release();
            if (rt != null) rt.Release();
        }

        protected float GetPaintedPercentage() {
            // 1. Create a tiny temporary RenderTexture
            // 64x64 is usually enough for a accurate score
            RenderTexture smallRT = RenderTexture.GetTemporary(64, 64);
            
            // 2. Downscale using Blit (GPU does the heavy lifting)
            // Use Bilinear filtering for better averaging of "painted" areas
            //rt.filterMode = FilterMode.Bilinear;
            Graphics.Blit(rt, smallRT);

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