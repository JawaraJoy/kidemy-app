using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HandTutorial : MonoBehaviour
{
    public enum TutorialMode
    {
        Normal,
        Click,
        Swipe
    }

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
    public void PlayTutorial(RectTransform startTarget, RectTransform endTarget, Transform canvasTransform, TutorialMode mode = TutorialMode.Normal)
    {
        if (handImagePrefab == null || startTarget == null || endTarget == null || canvasTransform == null) return;

        // Instantiate prefab tangan di Canvas
        handInstance = Instantiate(handImagePrefab, canvasTransform);
        RectTransform handRect = handInstance.GetComponent<RectTransform>();
        Image handImage = handInstance.GetComponent<Image>();
        Animator animator = handInstance.GetComponentInChildren<Animator>();
        Canvas canvas = canvasTransform.GetComponent<Canvas>();

        if (mode != TutorialMode.Swipe)
        {
            foreach (TrailRenderer trail in handInstance.GetComponentsInChildren<TrailRenderer>(true))
                trail.enabled = false;
        }

        if (canvas)
        {
            Canvas handCanvas = handInstance.GetComponent<Canvas>();
            if (!handCanvas)
                handCanvas = handInstance.AddComponent<Canvas>();

            handCanvas.overrideSorting = true;
            handCanvas.sortingLayerID = canvas.sortingLayerID;
            handCanvas.sortingOrder = canvas.sortingOrder + 2;
        }

        StartCoroutine(AnimateHand(handInstance, handRect, handImage, animator, canvas, startTarget, endTarget, mode));
    }

    private IEnumerator AnimateHand(GameObject handObj, RectTransform handRect, Image handImage, Animator animator, Canvas canvas, RectTransform startTarget, RectTransform endTarget, TutorialMode mode)
    {
        Vector3 startPos = startTarget.position;
        Vector3 endPos = endTarget.position;
        TrailRenderer trail = null;

        if (mode == TutorialMode.Swipe)
        {
            TrailRenderer prefabTrail = handObj.GetComponentInChildren<TrailRenderer>();
            if (prefabTrail)
            {
                GameObject trailOrigin = new GameObject("TrailOrigin");
                trailOrigin.transform.SetParent(handObj.transform, false);
                trailOrigin.transform.localPosition = Vector3.zero;

                trail = trailOrigin.AddComponent<TrailRenderer>();
                trail.time = prefabTrail.time;
                trail.widthMultiplier = prefabTrail.widthMultiplier;
                trail.widthCurve = prefabTrail.widthCurve;
                trail.colorGradient = prefabTrail.colorGradient;
                trail.material = prefabTrail.sharedMaterial;
                trail.alignment = prefabTrail.alignment;
                trail.textureMode = prefabTrail.textureMode;
                prefabTrail.enabled = false;
            }
            else
            {
                trail = handObj.AddComponent<TrailRenderer>();
            }

            if (canvas)
            {
                trail.sortingLayerID = canvas.sortingLayerID;
                trail.sortingOrder = canvas.sortingOrder + 1;
            }

            trail.enabled = false;

        }

        for (int i = 0; i < repeat; i++)
        {
            float elapsed = 0f;
            handRect.position = startPos;
            SetAlpha(handImage, 1f);

            if (mode == TutorialMode.Click)
                animator?.Play("Idle", 0, 0f);
            else if (mode == TutorialMode.Swipe)
            {
                trail.Clear();
                trail.enabled = false;
                animator?.Play("Idle", 0, 0f);
                yield return new WaitForSeconds(0.5f);
                yield return null;
                animator?.Play("Click", 0, 0f);
                yield return new WaitForSeconds(0.5f);
                trail.enabled = true;
            }

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / moveDuration;
                
                // Bergerak halus dari Pertanyaan ke Jawaban
                handRect.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            if (mode == TutorialMode.Swipe)
            {
                trail.enabled = false;
                animator?.Play("Idle", 0, 0f);
                yield return new WaitForSeconds(1f);
            }

            if (mode == TutorialMode.Click && animator)
            {
                animator.Play("Click", 0, 0f);
                yield return null;

                while (animator.GetCurrentAnimatorStateInfo(0).IsName("Click") &&
                       animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
                {
                    yield return null;
                }
            }

            // Fade Out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(handImage, Mathf.Lerp(1f, 0f, elapsed / fadeDuration));
                yield return null;
            }

            yield return new WaitForSeconds(mode == TutorialMode.Click ? 1f : 0.2f);
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
