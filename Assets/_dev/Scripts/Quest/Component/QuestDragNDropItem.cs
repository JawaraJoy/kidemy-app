using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using EasyTextEffects;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestDragNDropItem : QuestComponent, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
    {   

        [SerializeField] [FormerlySerializedAs("text")] protected TMP_Text m_Text;
        [SerializeField] [FormerlySerializedAs("audioPlayer")] private AudioPlayer m_AudioPlayer;
        [SerializeField] [FormerlySerializedAs("image")] private Image m_Image;
        [SerializeField] [FormerlySerializedAs("tint")] private Image m_Tint;
        
        protected LayoutElement m_LayoutElement;
        protected Vector2 m_OriginalPosition; 
        protected QuestDragNDropZone m_OriginalParent;
        protected CanvasGroup m_CanvasGroup;
        protected Vector2 m_OriginalSize;
        protected RectTransform m_ImageRect;

        [SerializeField] [FormerlySerializedAs("correct")] protected GameObject m_Correct;
        [SerializeField] [FormerlySerializedAs("wrong")] protected GameObject m_Wrong;

        [SerializeField, FormerlySerializedAs("container")] private Image m_Container;
        private QuestUtilLabel m_ItemData;
        public void SetContainerVisible(bool visible)
        {
            if (m_Container) m_Container.enabled = visible;
        }

        public QuestUtilLabel ItemData => m_ItemData;
        public QuestDragNDropZone OriginalZone => m_OriginalParent;

        protected override void Awake()
        {
            base.Awake();

            RegisterZoneInternal();

            ResetInternal();

            if(m_Image)
                m_Image.transform.TryGetComponent<RectTransform>(out m_ImageRect);

            if(m_ImageRect)
                m_OriginalSize = m_ImageRect.sizeDelta;
            else
                m_OriginalSize = Rect.sizeDelta;
        }

        protected override void Start()
        {
            base.Start();

            TryGetComponent<CanvasGroup>(out m_CanvasGroup);
        }

        public void OnPointerDown(PointerEventData eventData) { }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Vector3 position = Rect.position;
            if (Host?.Canvas)
            {
                Rect.SetParent(Host.Canvas.transform, true);
                Rect.anchorMin = Rect.anchorMax = Rect.pivot = Vector2.one * 0.5f;
                if (!m_ImageRect) Rect.sizeDelta = m_OriginalSize;
                Rect.position = position;
            }
            m_OriginalPosition = Rect.anchoredPosition;
            if (m_CanvasGroup) m_CanvasGroup.blocksRaycasts = false;
            if (m_ImageRect) m_ImageRect.sizeDelta = m_OriginalSize;
            if (m_Quest && ItemData?.Audio) m_Quest.PlayQuestionVoice(ItemData.Audio);
            if (m_Container) m_Container.enabled = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            float modifier = 1;

            if(Host != null)
                modifier = Host.Canvas.scaleFactor;

            Rect.anchoredPosition += eventData.delta / modifier;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (m_CanvasGroup) m_CanvasGroup.blocksRaycasts = true;
            if (OriginalZone && Rect.parent != OriginalZone.transform)
                OriginalZone.ReInsertItem(this);
        }

        public virtual void SetItem(QuestUtilLabel item)
        {
            m_ItemData = item;

            if(!string.IsNullOrEmpty(item.Text))
            {
                if (m_Text)
                {
                    m_Text.text = item.Text;
                    if (item.UseTextColor) m_Text.color = item.TextColor;
                    if (m_Text.TryGetComponent(out TextEffect effect)) effect.Refresh();
                }
            }   
            else
            {
                m_Text?.gameObject.SetActive(false);
                m_Text?.transform.parent.gameObject.SetActive(false);   
            }

            if(item.Audio)
            {
                if(m_AudioPlayer)
                    m_AudioPlayer.SetAudioClip(item.Audio);
            }   
            else
            {
                m_AudioPlayer?.gameObject.SetActive(false);
                m_AudioPlayer?.transform.parent.gameObject.SetActive(false);   
            }

            if(item.Image)
            {
                if(m_Image)
                    m_Image.sprite = item.Image;
            }   
            else
            {
                m_Image?.gameObject.SetActive(false);
                m_Image?.transform.parent.gameObject.SetActive(false);   
            }

            if(item.Color != Color.white && item.Color != Color.black)
            {
                if(m_Tint)
                    m_Tint.color = item.Color;
            }   
        }

        protected override void ResetInternal()
        {
            if(m_Correct)
                m_Correct.SetActive(false);

            if(m_Wrong)
                m_Wrong.SetActive(false);
        }

        public void RegisterZone()
        {
            RegisterZoneInternal();
        }

        protected virtual void RegisterZoneInternal()
        {
            transform.parent.TryGetComponent<QuestDragNDropZone>(out m_OriginalParent);
        }
    }
}