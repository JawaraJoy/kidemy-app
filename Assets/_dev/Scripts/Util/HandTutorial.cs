using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HandTutorial : MonoBehaviour
{
    public enum TutorialMode { Normal, Click, Swipe }

    private const string c_TrailOrigin = "TrailOrigin";
    private const string c_Idle = "Idle";
    private const string c_Click = "Click";
    private const float c_Hold = 0.5f;
    private const float c_ClickPause = 1f;
    private const float c_Pause = 0.2f;

    [SerializeField, FormerlySerializedAs("handImagePrefab")] private GameObject m_HandImagePrefab;
    [SerializeField, FormerlySerializedAs("repeat")] private int m_Repeat = 5;
    [SerializeField, FormerlySerializedAs("moveDuration")] private float m_MoveDuration = 1.2f;
    [SerializeField, FormerlySerializedAs("fadeDuration")] private float m_FadeDuration = 0.4f;

    private GameObject m_Hand;
    private Coroutine m_Animation;

    public void PlayTutorial(RectTransform start, RectTransform end, Transform canvas, TutorialMode mode = TutorialMode.Normal)
    {
        PlayInternal(new[] { start, end }, canvas, mode);
    }

    public void PlayTutorialCustom(RectTransform[] steps, Transform canvas)
    {
        PlayInternal(steps, canvas, TutorialMode.Click);
    }

    private void PlayInternal(RectTransform[] steps, Transform parent, TutorialMode mode)
    {
        if (!m_HandImagePrefab || !parent || steps == null || steps.Length < 2) return;
        foreach (var step in steps)
            if (!step) return;

        DisableInternal();
        m_Hand = Instantiate(m_HandImagePrefab, parent, false);
        if (!m_Hand.TryGetComponent(out RectTransform hand))
        {
            DisableInternal();
            return;
        }

        m_Hand.TryGetComponent(out Image image);
        if (image) image.raycastTarget = false;
        var animator = m_Hand.GetComponentInChildren<Animator>();
        parent.TryGetComponent(out Canvas canvas);
        if (canvas)
        {
            if (!m_Hand.TryGetComponent(out Canvas overlay)) overlay = m_Hand.AddComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingLayerID = canvas.sortingLayerID;
            overlay.sortingOrder = canvas.sortingOrder + 2;
        }

        TrailRenderer trail = null;
        var authored = m_Hand.GetComponentInChildren<TrailRenderer>(true);
        foreach (var existing in m_Hand.GetComponentsInChildren<TrailRenderer>(true)) existing.enabled = false;
        if (mode == TutorialMode.Swipe)
        {
            var origin = new GameObject(c_TrailOrigin);
            origin.transform.SetParent(hand, false);
            trail = origin.AddComponent<TrailRenderer>();
            if (authored)
            {
                trail.time = authored.time;
                trail.widthMultiplier = authored.widthMultiplier;
                trail.widthCurve = authored.widthCurve;
                trail.colorGradient = authored.colorGradient;
                trail.sharedMaterial = authored.sharedMaterial;
                trail.alignment = authored.alignment;
                trail.textureMode = authored.textureMode;
            }
            if (canvas)
            {
                trail.sortingLayerID = canvas.sortingLayerID;
                trail.sortingOrder = canvas.sortingOrder + 1;
            }
            trail.enabled = false;
        }
        m_Animation = StartCoroutine(Animate(steps, mode, hand, image, animator, trail));
    }

    private IEnumerator Animate(RectTransform[] steps, TutorialMode mode, RectTransform hand, Image image, Animator animator, TrailRenderer trail)
    {
        for (int repeat = 0; repeat < m_Repeat && steps[0]; repeat++)
        {
            hand.position = steps[0].position;
            SetAlpha(image, 1f);
            if (animator) animator.Play(c_Idle);
            if (trail)
            {
                trail.Clear();
                yield return new WaitForSeconds(c_Hold);
                if (animator) animator.Play(c_Click);
                yield return new WaitForSeconds(c_Hold);
                trail.enabled = true;
            }

            for (int i = 1; i < steps.Length; i++)
            {
                if (!steps[i]) break;
                Vector3 start = hand.position;
                Vector3 end = steps[i].position;
                for (float elapsed = 0f; elapsed < m_MoveDuration; elapsed += Time.deltaTime)
                {
                    hand.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, elapsed / m_MoveDuration));
                    yield return null;
                }
                hand.position = end;
                if (mode == TutorialMode.Click && animator)
                {
                    animator.Play(c_Click, 0, 0f);
                    yield return null;
                    while (animator.GetCurrentAnimatorStateInfo(0).IsName(c_Click) &&
                           animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f) yield return null;
                    animator.Play(c_Idle);
                }
            }

            if (trail)
            {
                trail.enabled = false;
                if (animator) animator.Play(c_Idle);
                yield return new WaitForSeconds(c_ClickPause);
            }
            for (float elapsed = 0f; elapsed < m_FadeDuration; elapsed += Time.deltaTime)
            {
                SetAlpha(image, 1f - elapsed / m_FadeDuration);
                yield return null;
            }
            SetAlpha(image, 0f);
            yield return new WaitForSeconds(mode == TutorialMode.Click ? c_ClickPause : c_Pause);
        }
        m_Animation = null;
        if (m_Hand) Destroy(m_Hand);
        m_Hand = null;
    }

    private void SetAlpha(Image image, float alpha)
    {
        if (!image) return;
        Color tint = image.color;
        tint.a = alpha;
        image.color = tint;
    }

    public void Disable() => DisableInternal();
    private void OnDisable() => DisableInternal();

    private void DisableInternal()
    {
        if (m_Animation != null) StopCoroutine(m_Animation);
        m_Animation = null;
        if (m_Hand)
        {
            m_Hand.SetActive(false);
            Destroy(m_Hand);
            m_Hand = null;
        }
    }
}
