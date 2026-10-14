using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class HandTutorial : MonoBehaviour
{
    private const string c_TrailOriginName = "TrailOrigin";
    private const float c_ClickWaitSeconds = 1f;
    private const float c_DefaultWaitSeconds = 0.2f;
    private const string c_IdleAnimationName = "Idle";
    private const string c_ClickAnimationName = "Click";

    public enum TutorialMode
    {
        Normal,
        Click,
        Swipe
    }

    [Header("Hand Prefab Settings")]
    [SerializeField] [FormerlySerializedAs("handImagePrefab")] private GameObject m_HandImagePrefab; // Assign UI Image Tangan di Inspector
    [SerializeField] [FormerlySerializedAs("repeat")] private int m_Repeat = 5;

    [Header("Animation Settings")]
    [SerializeField] [FormerlySerializedAs("moveDuration")] private float m_MoveDuration = 1.2f;
    [SerializeField] [FormerlySerializedAs("fadeDuration")] private float m_FadeDuration = 0.4f;

    private readonly string m_IdleAnimationName = c_IdleAnimationName;
    private readonly string m_ClickAnimationName = c_ClickAnimationName;

    private GameObject m_HandInstance;

    /// <summary>
    /// Memasang (Instantiate) tangan ke Canvas dan menjalankan animasinya
    /// </summary>
    public void PlayTutorial(RectTransform startTarget, RectTransform endTarget, Transform canvasTransform, TutorialMode mode = TutorialMode.Normal)
    {
        if (m_HandImagePrefab == null || startTarget == null || endTarget == null || canvasTransform == null) return;

        // Instantiate prefab tangan di Canvas
        m_HandInstance = Instantiate(m_HandImagePrefab, canvasTransform);
        m_HandInstance.TryGetComponent<RectTransform>(out RectTransform handRect);
        m_HandInstance.TryGetComponent<Image>(out Image handImage);
        Animator animator = m_HandInstance.GetComponentInChildren<Animator>();
        canvasTransform.TryGetComponent<Canvas>(out Canvas canvas);

        if (mode != TutorialMode.Swipe)
        {
            foreach (TrailRenderer trail in m_HandInstance.GetComponentsInChildren<TrailRenderer>(true))
                trail.enabled = false;
        }

        if (canvas)
        {
            m_HandInstance.TryGetComponent<Canvas>(out Canvas handCanvas);
            if (!handCanvas)
                handCanvas = m_HandInstance.AddComponent<Canvas>();

            handCanvas.overrideSorting = true;
            handCanvas.sortingLayerID = canvas.sortingLayerID;
            handCanvas.sortingOrder = canvas.sortingOrder + 2;
        }

        StartCoroutine(AnimateHand(m_HandInstance, handRect, handImage, animator, canvas, startTarget, endTarget, mode));
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
                GameObject trailOrigin = new GameObject(c_TrailOriginName);
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

        for (int i = 0; i < m_Repeat; i++)
        {
            float elapsed = 0f;
            if (handRect == null) yield break;
            handRect.position = startPos;
            SetAlpha(handImage, 1f);

            if (mode == TutorialMode.Click)
                animator?.Play(m_IdleAnimationName, 0, 0f);
            else if (mode == TutorialMode.Swipe)
            {
                trail.Clear();
                trail.enabled = false;
                animator?.Play(m_IdleAnimationName, 0, 0f);
                yield return new WaitForSeconds(0.5f);
                yield return null;
                animator?.Play(m_ClickAnimationName, 0, 0f);
                yield return new WaitForSeconds(0.5f);
                trail.enabled = true;
            }

            while (elapsed < m_MoveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / m_MoveDuration;
                
                // Bergerak halus dari Pertanyaan ke Jawaban
                if (handRect != null)
                {
                    handRect.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
                }
                
                yield return null;
            }

            if (mode == TutorialMode.Swipe)
            {
                trail.enabled = false;
                animator?.Play(m_IdleAnimationName, 0, 0f);
                yield return new WaitForSeconds(1f);
            }

            if (mode == TutorialMode.Click && animator)
            {
                animator.Play(m_ClickAnimationName, 0, 0f);
                yield return null;

                while (animator.GetCurrentAnimatorStateInfo(0).IsName(m_ClickAnimationName) &&
                       animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
                {
                    yield return null;
                }
            }

            // Fade Out
            elapsed = 0f;
            while (elapsed < m_FadeDuration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(handImage, Mathf.Lerp(1f, 0f, elapsed / m_FadeDuration));
                yield return null;
            }

            yield return new WaitForSeconds(mode == TutorialMode.Click ? c_ClickWaitSeconds : c_DefaultWaitSeconds);
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
        if(m_HandInstance)
        {
            Destroy(m_HandInstance);
        }
    }
}
