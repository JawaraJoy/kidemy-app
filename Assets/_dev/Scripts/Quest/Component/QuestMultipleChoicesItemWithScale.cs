using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMultipleChoicesItemWithScale : QuestMultipleChoicesItem
    {
        public override void SetChoice(QuestUtilLabelChoice choice)
        {
            this.m_Choice = choice;

            if(!string.IsNullOrEmpty(choice.Text))
            {
                if(m_Text)
                    m_Text.text = choice.Text;
            }   
            else
            {
                m_Text?.gameObject.SetActive(false);
                m_Text?.transform.parent.gameObject.SetActive(false);   
            }

            if(choice.Audio)
            {
                if(m_AudioPlayer)
                    m_AudioPlayer.SetAudioClip(choice.Audio);
            }   
            else
            {
                m_AudioPlayer?.gameObject.SetActive(false);
                m_AudioPlayer?.transform.parent.gameObject.SetActive(false);   
            }

            if(choice.Image)
            {
                if(m_Image)
                {
                    RectTransform imageRect = m_Image.transform.GetComponent<RectTransform>();

                    if(imageRect)
                    {
                        imageRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, choice.Image.rect.size.x);
                        imageRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, choice.Image.rect.size.y);

                        imageRect.anchorMax = Vector2.one * 0.5f;
                        imageRect.anchorMin = Vector2.one * 0.5f;
                        imageRect.anchoredPosition = Vector2.zero;

                        imageRect.pivot = Vector2.one * 0.5f;
                    }

                    m_Image.sprite = choice.Image;   
                }
            }   
            else
            {
                m_Image?.gameObject.SetActive(false);
                m_Image?.transform.parent.gameObject.SetActive(false);   
            }
        }
    }
}