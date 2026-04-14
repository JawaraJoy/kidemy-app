using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMultipleChoicesItemWithScale : QuestMultipleChoicesItem
    {
        public override void SetChoice(QuestUtilLabelChoice choice)
        {
            this.choice = choice;

            if(!string.IsNullOrEmpty(choice.Text))
            {
                if(text)
                    text.text = choice.Text;
            }   
            else
            {
                text?.gameObject.SetActive(false);
                text?.transform.parent.gameObject.SetActive(false);   
            }

            if(choice.Audio)
            {
                if(audioPlayer)
                    audioPlayer.SetAudioClip(choice.Audio);
            }   
            else
            {
                audioPlayer?.gameObject.SetActive(false);
                audioPlayer?.transform.parent.gameObject.SetActive(false);   
            }

            if(choice.Image)
            {
                if(image)
                {
                    RectTransform imageRect = image.transform.GetComponent<RectTransform>();

                    if(imageRect)
                    {
                        imageRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, choice.Image.rect.size.x);
                        imageRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, choice.Image.rect.size.y);

                        imageRect.anchorMax = Vector2.one * 0.5f;
                        imageRect.anchorMin = Vector2.one * 0.5f;
                        imageRect.anchoredPosition = Vector2.zero;

                        imageRect.pivot = Vector2.one * 0.5f;
                    }

                    image.sprite = choice.Image;   
                }
            }   
            else
            {
                image?.gameObject.SetActive(false);
                image?.transform.parent.gameObject.SetActive(false);   
            }
        }
    }
}