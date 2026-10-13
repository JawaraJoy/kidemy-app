using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestDragNDropSlot : QuestComponent
    {   
        [SerializeField] [FormerlySerializedAs("text")] protected TMP_Text m_Text;
        [SerializeField] [FormerlySerializedAs("audioPlayer")] private AudioPlayer m_AudioPlayer;
        [SerializeField] [FormerlySerializedAs("image")] private Image m_Image;
        [SerializeField] [FormerlySerializedAs("tints")] private Image[] m_Tints;
        [SerializeField] [FormerlySerializedAs("dropZone")] private QuestDragNDropZone m_DropZone;
        [SerializeField] [FormerlySerializedAs("orderLabel")] private TMP_Text m_OrderLabel;
        [SerializeField] [FormerlySerializedAs("step")] private Transform m_Step;
        
        private QuestUtilLabelGroup m_GroupData;
        public QuestUtilLabelGroup GroupData => m_GroupData;

        public QuestDragNDropZone DropZone => m_DropZone;

        private QuestDragNDrop m_QuestDragNDrop;

        private Color[] m_LabelColor = new Color[]
        {
            Color.green,
            Color.orange,
            Color.blue
        };

        protected override void Start()
        {
            m_QuestDragNDrop = GetComponentInParent<QuestDragNDrop>();

            m_DropZone = GetComponentInChildren<QuestDragNDropZone>();
            
            if(!m_DropZone)
                Debug.LogError("QuestDragNDropZone not found in childern");

            if(m_QuestDragNDrop)
                m_Quest = m_QuestDragNDrop;
            else
                Debug.LogError("QuestDragNDrop not found in parent");
        }

        public virtual void SetSlot(QuestUtilLabelGroup item, int order = 0, bool isLast = false)
        {
            m_GroupData = item;

            if(!string.IsNullOrEmpty(item.Label.Text))
            {
                if(m_Text)
                    m_Text.text = item.Label.Text;
            }   
            else
            {
                m_Text?.gameObject.SetActive(false);
                m_Text?.transform.parent.gameObject.SetActive(false);   
            }

            if(item.Label.Audio)
            {
                if(m_AudioPlayer)
                    m_AudioPlayer.SetAudioClip(item.Label.Audio);
            }   
            else
            {
                m_AudioPlayer?.gameObject.SetActive(false);
                m_AudioPlayer?.transform.parent.gameObject.SetActive(false);   
            }

            if(item.Label.Image)
            {
                if(m_Image)
                    m_Image.sprite = item.Label.Image;
            }   
            else
            {
                m_Image?.gameObject.SetActive(false);
                m_Image?.transform.parent.gameObject.SetActive(false);   
            }

            if(order > 0 && m_OrderLabel)
                m_OrderLabel.SetText(order.ToString());
            
            if(m_Tints.Length > 0)
                foreach(var tint in m_Tints)
                    tint.color = item.Label.Color;
                
            if(m_Step && isLast)
                m_Step.gameObject.SetActive(false);
        }
    }
}