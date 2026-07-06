using UnityEngine;
using UnityEngine.UI;

public class RawImageAlphaFilter : MonoBehaviour, ICanvasRaycastFilter
{
    [Range(0, 1)]
    public float alphaThreshold = 0.1f;
    
    // We will use the original static texture for the "hit test"
    private Texture2D _maskTexture;
    private RawImage _rawImage;

    void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        
        // IMPORTANT: Grab the texture BEFORE it gets swapped with the RenderTexture
        // Or assign the base texture manually in the inspector.
        if (_rawImage.texture is Texture2D tex)
        {
            _maskTexture = tex;
        }
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (_maskTexture == null || alphaThreshold <= 0) return true;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rawImage.rectTransform, screenPoint, eventCamera, out localPoint))
        {
            return false;
        }

        Rect rect = _rawImage.rectTransform.rect;
        Vector2 normalizedUV = new Vector2(
            (localPoint.x - rect.x) / rect.width,
            (localPoint.y - rect.y) / rect.height
        );

        // We sample the STATIC texture (CPU-accessible) instead of the RenderTexture
        try
        {
            return _maskTexture.GetPixelBilinear(normalizedUV.x, normalizedUV.y).a >= alphaThreshold;
        }
        catch (UnityException)
        {
            Debug.LogError("The Base Texture must have 'Read/Write Enabled' checked in Import Settings!");
            return true;
        }
    }
}
