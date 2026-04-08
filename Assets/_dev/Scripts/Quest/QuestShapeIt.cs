using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestShapeIt : Quest
    {
        [Header("Components")]
        [SerializeField] private Image questionImage;
        [SerializeField] private AudioPlayer questionAudio;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private QuestDragNDropZone itemsContainer;
        [SerializeField] private RectTransform zonesContainer;

        private SO_QuestShapeIt dataShapeIt;
        private QuestShapeItCanvas canvas;
        private QuestDragNDropItem[] items;

        public override void Init(SO_Quest data)
        {
            dataShapeIt = data as SO_QuestShapeIt;

            if(dataShapeIt)
                base.Init(data);
            else
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestShapeIt");

            if (questionImage)
            {
                if (dataShapeIt.Question.Image)
                    questionImage.sprite = dataShapeIt.Question.Image;
                else
                {
                    questionImage.gameObject.SetActive(false);
                    questionImage.transform.parent.gameObject.SetActive(false);
                }
            }

            if (questionAudio)
            {
                if (dataShapeIt.Question.Audio)
                    questionAudio.SetAudioClip(dataShapeIt.Question.Audio);
                else
                {
                    questionAudio.gameObject.SetActive(false);
                    questionAudio.transform.parent.gameObject.SetActive(false);
                }
            }

            if (questionText)
            {
                if (!string.IsNullOrEmpty(dataShapeIt.Question.Text))
                    questionText.text = dataShapeIt.Question.Text;
                else
                {
                    questionText.gameObject.SetActive(false);
                    questionText.transform.parent.gameObject.SetActive(false);
                }
            }

            if (zonesContainer && dataShapeIt.Canvas != null)
                canvas = Instantiate<QuestShapeItCanvas>(dataShapeIt.Canvas, zonesContainer);
            
            if (itemsContainer)
            {
                items = new QuestDragNDropItem[canvas.Items.Length];

                for (int i = 0; i < canvas.Items.Length; i++)
                    items[i] = InstantiateItem(canvas.Items[i]);
            }

            if(nextButton && dataShapeIt.AutoSubmit)
                nextButton.gameObject.SetActive(false);
        }

        QuestDragNDropItem InstantiateItem(QuestDragNDropItem item)
        {
            itemsContainer.ReInsertItem(item);
            item.transform.localPosition = Vector3.zero;
            item.Rect.localScale = Vector3.one;

            return item;
        }

        public override void Reset()
        {
            foreach (var choice in items)
                choice.Reset();

            if (restartButton)
                restartButton.gameObject.SetActive(false);
        }

        void RecalculateContainer()
        {
            if (items[0].Rect.sizeDelta.x > 0 && items[0].Rect.sizeDelta.y > 0)
            {
                GridLayoutGroup gridLayoutGroup = itemsContainer.GetComponent<GridLayoutGroup>();
                if (gridLayoutGroup)
                    gridLayoutGroup.cellSize = items[0].Rect.sizeDelta;
            }

            QuestDragNDropZone questDragNDropZone = itemsContainer.GetComponent<QuestDragNDropZone>();
                
            if(questDragNDropZone)
                questDragNDropZone.RegisterItems();
        }

        public override void Enabled()
        {
            base.Enabled();

            //Invoke("RecalculateContainer", 0.5f);
        }
    }
}