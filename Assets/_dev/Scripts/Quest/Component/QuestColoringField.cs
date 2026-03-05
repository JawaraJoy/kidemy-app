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
                Texture baseTexture = image.texture;

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

        private void OnDestroy()
        {
            cb?.Release();
            if (rt != null) rt.Release();
        }
    }
}