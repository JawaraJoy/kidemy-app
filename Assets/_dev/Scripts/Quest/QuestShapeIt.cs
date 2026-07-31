using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestShapeIt : QuestZonePairing
    {
        [Header("Components")]
        [SerializeField] private Image questionImage;
        [SerializeField] private AudioPlayer questionAudio;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private QuestDragNDropZone itemsContainer;
        [SerializeField] private QuestShapeItCanvas canvas;
        [SerializeField] private Button rotateButton;

        [Header("Feedback")]
        [SerializeField] private AudioClip rotateSound;

        private SO_QuestShapeIt dataShapeIt;
        private Dictionary<int, int> itemIndex = new Dictionary<int, int>();
        private int unansweredQuestion = 0;
        private IEnumerator rotateCo;

        protected override void Start()
        {
            base.Start();

            dataShapeIt = data as SO_QuestShapeIt;

            if(!dataShapeIt)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestShapeIt");

            if(!canvas)
                Debug.LogError("Canvas not found");

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

            if(rotateButton)
            {
                ButtonEvents buttonEvents = rotateButton.GetComponent<ButtonEvents>();

                if(buttonEvents)
                {
                    buttonEvents.AddEventOnPointerOver(FeedbackManager.Instance.ButtonOver.Play);
                    buttonEvents.AddEventOnPointerExit(FeedbackManager.Instance.ButtonExit.Play);
                    buttonEvents.AddEventOnPointerClick(FeedbackManager.Instance.ButtonClick.Play);
                }
            }

            if (canvas && itemsContainer)
                Reset();

            SetDialog();

            base.Reset();
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
            foreach (var shape in canvas.Items)
            {
                Transform shapeParent = shape.transform.parent;
                QuestDragNDropZone shapeZone = shapeParent.GetComponent<QuestDragNDropZone>();

                if (shapeZone)
                {
                    if(!itemIndex.ContainsKey(shape.transform.GetInstanceID()))
                    {
                        shape.RegisterZone();
                        itemIndex.Add(shape.transform.GetInstanceID(), shapeZone.transform.GetInstanceID());
                    }

                    itemsContainer.AddItem(shape);

                    if(shape.CanBeRotated)
                        shape.Rect.localRotation = Quaternion.Euler(Vector3.forward * (Random.Range(0, 4) * 90));
                }
            }

            unansweredQuestion = canvas.Items.Length;    

            SetDialog();
        }

        void RecalculateContainer()
        {
            QuestDragNDropZone questDragNDropZone = itemsContainer.GetComponent<QuestDragNDropZone>();
                
            if(questDragNDropZone)
                questDragNDropZone.RegisterItems();
        }

        public override void Enabled()
        {
            base.Enabled();

            //Invoke("RecalculateContainer", 0.5f);
        }

        public override bool Verify(QuestDragNDropItem item, QuestDragNDropZone zone)
        {
            bool ret = false;

            int id = item.transform.GetInstanceID();

            if(itemIndex.ContainsKey(id))
                ret = itemIndex[id] == zone.transform.GetInstanceID() && item.Rect.eulerAngles.z < 0.01f;

            return ret;
        }

        public override void OnAnswered(bool answer, bool submit = true)
        {
            unansweredQuestion--;

            if(unansweredQuestion <= 0)
                Submit(3);
        }

        public virtual void Rotate()
        {
            if(rotateCo == null)
            {
                rotateCo = RotateCo();

                StartCoroutine(rotateCo);
            }
        }

        IEnumerator RotateCo()
        {
            float rotateValue = 90f;

            if(rotateSound)
                GameManager.Instance.AudioSource.PlayOneShot(rotateSound);
            
            while (rotateValue > 0)
            {
                foreach (var item in canvas.Items)
                {
                    if(!item.CanBeRotated || item.transform.parent.GetInstanceID() != itemsContainer.transform.GetInstanceID())
                        continue;

                    item.Rect.localRotation = Quaternion.Euler(Vector3.forward * (item.Rect.eulerAngles.z + 3));

                    if(item.Rect.eulerAngles.z == 360)
                        item.Rect.localRotation = Quaternion.Euler(Vector3.zero);
                }

                rotateValue -= 3;

                yield return new WaitForSeconds(Time.deltaTime);
            }

            rotateCo = null;
        }

        void SetDialog()
        {
            if (!string.IsNullOrEmpty(dataShapeIt.Question.Text))
                GameManager.Instance.SetNPCDialog(dataShapeIt.Question.Text);
        }
    }
}