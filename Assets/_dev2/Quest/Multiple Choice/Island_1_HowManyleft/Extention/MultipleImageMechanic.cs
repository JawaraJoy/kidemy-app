using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    [System.Serializable]
    public class MultipleImageMechanic
    {
        [SerializeField]
        private Sprite m_Illustraion;
        [SerializeField]
        private bool m_Gone;

        [Header("Fade Away")]
        [SerializeField, Range(0f, 1f)] private float m_GhostAlpha = 0.5f;
        [SerializeField] private float m_FadeAwayDuration = 1f;
        [SerializeField] private float m_FloatUpDistance = 100f;

        public Sprite Illustration => m_Illustraion;
        public bool Gone => m_Gone;
        public void Init(Image image)
        {
            GameManager.Instance.StartCoroutine(StartingAnimateFadeAway(image));
        }

        private IEnumerator StartingAnimateFadeAway(Image image)
        {
            if (!m_Gone) yield break;
            yield return new WaitForSeconds(1.3f);

            // duplikat illustration di parent yang sama, lalu ignore layout
            Image clone =GameObject.Instantiate(image, image.transform.parent);
            if (!clone.TryGetComponent(out LayoutElement layoutElement))
                layoutElement = clone.gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            clone.raycastTarget = false;

            // salin ukuran & posisi hasil layout dari image asli
            // (Instantiate tidak membawa nilai yang di-drive layout group, hasilnya size 0x0)
            RectTransform src = image.rectTransform;
            RectTransform dst = clone.rectTransform;
            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.sizeDelta = src.sizeDelta;
            dst.anchoredPosition = src.anchoredPosition;


            // illustrasi asli jadi setengah transparan
            SetAlpha(image, m_GhostAlpha);

            // duplikat naik pelan-pelan sambil fade out
            RectTransform cloneRect = clone.rectTransform;
            Vector2 startPos = cloneRect.anchoredPosition;
            Vector2 endPos = startPos + Vector2.up * m_FloatUpDistance;
            float startAlpha = clone.color.a;

            float elapsed = 0f;
            while (elapsed < m_FadeAwayDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / m_FadeAwayDuration);
                cloneRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                SetAlpha(clone, Mathf.Lerp(startAlpha, 0f, t));
                yield return null;
            }

            // sudah alpha 0, hapus duplikatnya
            GameObject.Destroy(clone.gameObject);
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }
    }
}