using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace EduGame
{
    public class QuestDragNDropSlot : QuestComponent
    {   
        [SerializeField] protected TMP_Text text;
        [SerializeField] private AudioPlayer audioPlayer;
        [SerializeField] private Image image;
        [SerializeField] private Image[] tints;
        [SerializeField] private QuestDragNDropZone dropZone;
        [SerializeField] private TMP_Text orderLabel;
        [SerializeField] private Transform step;
        
        public QuestUtilLabelGroup GroupData { get; private set; }

        public QuestDragNDropZone DropZone => dropZone;

        private QuestDragNDrop questDragNDrop;

        private Color[] labelColor = new Color[]
        {
            Color.green,
            Color.orange,
            Color.blue
        };

        protected override void Start()
        {
            questDragNDrop = GetComponentInParent<QuestDragNDrop>();

            dropZone = GetComponentInChildren<QuestDragNDropZone>();
            
            if(!dropZone)
                Debug.LogError("QuestDragNDropZone not found in childern");

            if(questDragNDrop)
                quest = questDragNDrop;
            else
                Debug.LogError("QuestDragNDrop not found in parent");
        }

        public virtual void SetSlot(QuestUtilLabelGroup item, int order = 0, bool isLast = false)
        {
            GroupData = item;

            if(!string.IsNullOrEmpty(item.Label.Text))
            {
                if(text)
                    text.text = item.Label.Text;
            }   
            else
            {
                text?.gameObject.SetActive(false);
                text?.transform.parent.gameObject.SetActive(false);   
            }

            if(item.Label.Audio)
            {
                if(audioPlayer)
                    audioPlayer.SetAudioClip(item.Label.Audio);
            }   
            else
            {
                audioPlayer?.gameObject.SetActive(false);
                audioPlayer?.transform.parent.gameObject.SetActive(false);   
            }

            if(item.Label.Image)
            {
                if(image)
                    image.sprite = item.Label.Image;
            }   
            else
            {
                image?.gameObject.SetActive(false);
                image?.transform.parent.gameObject.SetActive(false);   
            }

            if(order > 0 && orderLabel)
                orderLabel.SetText(order.ToString());
            
            if(tints.Length > 0)
                foreach(var tint in tints)
                    tint.color = item.Label.Color;
                
            if(step && isLast)
                step.gameObject.SetActive(false);
        }
    }
}