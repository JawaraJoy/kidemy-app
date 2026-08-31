using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HandTutorial : MonoBehaviour
{
    [Header("Hand Prefab Settings")]
    [SerializeField] private GameObject handImagePrefab; // Assign UI Image Tangan di Inspector
    [SerializeField] private int repeat = 5;

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 1.2f;
    [SerializeField] private float fadeDuration = 0.4f;

    private GameObject handInstance;

    /// <summary>
    /// Memasang (Instantiate) tangan ke Canvas dan menjalankan animasinya
    /// </summary>
    public void PlayTutorial(RectTransform startTarget, RectTransform endTarget, Transform canvasTransform)
    {
        if (handImagePrefab == null || startTarget == null || endTarget == null) return;

        // Instantiate prefab tangan di Canvas
        handInstance = Instantiate(handImagePrefab, canvasTransform);
        RectTransform handRect = handInstance.GetComponent<RectTransform>();
        Image handImage = handInstance.GetComponent<Image>();

        StartCoroutine(AnimateHand(handInstance, handRect, handImage, startTarget, endTarget));
    }

    private IEnumerator AnimateHand(GameObject handObj, RectTransform handRect, Image handImage, RectTransform startTarget, RectTransform endTarget)
    {
        Vector3 startPos = startTarget.position;
        Vector3 endPos = endTarget.position;

        // Animasi diulang 2 kali
        for (int i = 0; i < repeat; i++)
        {
            float elapsed = 0f;
            handRect.position = startPos;
            SetAlpha(handImage, 1f);

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / moveDuration;
                
                // Bergerak halus dari Pertanyaan ke Jawaban
                handRect.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            // Fade Out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(handImage, Mathf.Lerp(1f, 0f, elapsed / fadeDuration));
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
        }

        // Hancurkan objek tangan setelah animasi selesai
        Destroy(handObj);
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img != null)
        {
            Color color = img.color;
            color.a = alpha;
            img.color = color;
        }
    }

    public void Disable()
    {
        if(handInstance)
        {
            Destroy(handInstance);
        }
    }
}