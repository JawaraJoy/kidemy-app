using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace EduGame
{
    public class QuestDragNDropItem : QuestComponent, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
    {   

        [SerializeField] protected TMP_Text text;
        [SerializeField] private AudioPlayer audioPlayer;
        [SerializeField] private Image image;
        
        protected LayoutElement layoutElement;
        protected Vector2 originalPosition; 
        protected QuestDragNDropZone originalParent;
        protected CanvasGroup  canvasGroup;

        [SerializeField] protected GameObject correct;
        [SerializeField] protected GameObject wrong;

        public QuestUtilLabel ItemData { get; private set; }
        public QuestDragNDropZone OriginalZone => originalParent;

        protected override void Awake()
        {
            base.Awake();

            Reset();
        }

        protected override void Start()
        {
            base.Start();

            RegisterZone();

            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if(ChallengeManager.Instance)
                transform.parent = ChallengeManager.Instance.Canvas.transform;

            originalPosition = Rect.anchoredPosition;

            canvasGroup.blocksRaycasts = false;

            originalParent.Scaled(this, 1);
        }

        public void OnDrag(PointerEventData eventData)
        {
            float modifier = 1;

            if(ChallengeManager.Instance)
                modifier = ChallengeManager.Instance.Canvas.scaleFactor;

            Rect.anchoredPosition += eventData.delta / modifier;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;

            if(eventData.pointerEnter == null || !eventData.pointerEnter.GetComponent<QuestDragNDropZone>())
                OriginalZone.ReInsertItem(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            //throw new System.NotImplementedException();
        }

        public virtual void SetItem(QuestUtilLabel item)
        {
            ItemData = item;

            if(!string.IsNullOrEmpty(item.Text))
            {
                if(text)
                    text.text = item.Text;
            }   
            else
            {
                text?.gameObject.SetActive(false);
                text?.transform.parent.gameObject.SetActive(false);   
            }

            if(item.Audio)
            {
                if(audioPlayer)
                    audioPlayer.SetAudioClip(item.Audio);
            }   
            else
            {
                audioPlayer?.gameObject.SetActive(false);
                audioPlayer?.transform.parent.gameObject.SetActive(false);   
            }

            if(item.Image)
            {
                if(image)
                    image.sprite = item.Image;
            }   
            else
            {
                image?.gameObject.SetActive(false);
                image?.transform.parent.gameObject.SetActive(false);   
            }
        }

        public override void Reset()
        {
            if(correct)
                correct.SetActive(false);

            if(wrong)
                wrong.SetActive(false);
        }

        public virtual void RegisterZone()
        {
            originalParent = transform.parent.GetComponent<QuestDragNDropZone>();
        }
    }
}