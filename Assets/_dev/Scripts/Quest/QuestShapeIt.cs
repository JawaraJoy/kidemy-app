using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestShapeIt : QuestZonePairing
    {
        private const int c_RotationStepCount = 4;
        private const int c_QuarterTurn = 90;
        private const float c_AngleEpsilon = 0.01f;
        private const int c_RotateStepDegrees = 3;
        private const int c_FullCircle = 360;

        [Header("Components")]
        [SerializeField] [FormerlySerializedAs("questionImage")] private Image m_QuestionImage;
        [SerializeField] [FormerlySerializedAs("questionAudio")] private AudioPlayer m_QuestionAudio;
        [SerializeField] [FormerlySerializedAs("questionText")] private TMP_Text m_QuestionText;
        [SerializeField] [FormerlySerializedAs("itemsContainer")] private QuestDragNDropZone m_ItemsContainer;
        [SerializeField] [FormerlySerializedAs("canvas")] private QuestShapeItCanvas m_Canvas;
        [SerializeField] [FormerlySerializedAs("rotateButton")] private Button m_RotateButton;

        [Header("Feedback")]
        [SerializeField] [FormerlySerializedAs("rotateSound")] private AudioClip m_RotateSound;

        private SO_QuestShapeIt m_DataShapeIt;
        private Dictionary<EntityId, EntityId> m_ItemIndex = new Dictionary<EntityId, EntityId>();
        private int m_UnansweredQuestion = 0;
        private IEnumerator m_RotateCo;

        protected override void Start()
        {
            base.Start();

            m_DataShapeIt = m_Data as SO_QuestShapeIt;

            if(!m_DataShapeIt)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestShapeIt");

            if(!m_Canvas)
                Debug.LogError("Canvas not found");

            if (m_QuestionImage)
            {
                if (m_DataShapeIt.Question.Image)
                    m_QuestionImage.sprite = m_DataShapeIt.Question.Image;
                else
                {
                    m_QuestionImage.gameObject.SetActive(false);
                    m_QuestionImage.transform.parent.gameObject.SetActive(false);
                }
            }

            if (m_QuestionAudio)
            {
                if (m_DataShapeIt.Question.Audio)
                    m_QuestionAudio.SetAudioClip(m_DataShapeIt.Question.Audio);
                else
                {
                    m_QuestionAudio.gameObject.SetActive(false);
                    m_QuestionAudio.transform.parent.gameObject.SetActive(false);
                }
            }

            if (m_QuestionText)
            {
                if (!string.IsNullOrEmpty(m_DataShapeIt.Question.Text))
                    m_QuestionText.text = m_DataShapeIt.Question.Text;
                else
                {
                    m_QuestionText.gameObject.SetActive(false);
                    m_QuestionText.transform.parent.gameObject.SetActive(false);
                }
            }

            if(m_RotateButton)
            {
                m_RotateButton.TryGetComponent<ButtonEvents>(out ButtonEvents buttonEvents);

                if(buttonEvents)
                {
                    buttonEvents.AddEventOnPointerOver(FeedbackManager.Instance.ButtonOver.Play);
                    buttonEvents.AddEventOnPointerExit(FeedbackManager.Instance.ButtonExit.Play);
                    buttonEvents.AddEventOnPointerClick(FeedbackManager.Instance.ButtonClick.Play);
                }
            }

            if (m_Canvas && m_ItemsContainer)
                ResetInternal();

            SetDialog();

            base.ResetInternal();
        }

        QuestDragNDropItem InstantiateItem(QuestDragNDropItem item)
        {
            m_ItemsContainer.ReInsertItem(item);
            item.transform.localPosition = Vector3.zero;
            item.Rect.localScale = Vector3.one;

            return item;
        }

        protected override void ResetInternal()
        {
            foreach (var shape in m_Canvas.Items)
            {
                Transform shapeParent = shape.transform.parent;
                shapeParent.TryGetComponent<QuestDragNDropZone>(out QuestDragNDropZone shapeZone);

                if (shapeZone)
                {
                    if(!m_ItemIndex.ContainsKey(shape.transform.GetEntityId()))
                    {
                        shape.RegisterZone();
                        m_ItemIndex.Add(shape.transform.GetEntityId(), shapeZone.transform.GetEntityId());
                    }

                    m_ItemsContainer.AddItem(shape);

                    if(shape.CanBeRotated)
                        shape.Rect.localRotation = Quaternion.Euler(Vector3.forward * (Random.Range(0, c_RotationStepCount) * c_QuarterTurn));
                }
            }

            m_UnansweredQuestion = m_Canvas.Items.Length;    

            SetDialog();
        }

        void RecalculateContainer()
        {
            m_ItemsContainer.TryGetComponent<QuestDragNDropZone>(out QuestDragNDropZone questDragNDropZone);
                
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

            var id = item.transform.GetEntityId();

            if(m_ItemIndex.ContainsKey(id))
                ret = m_ItemIndex[id] == zone.transform.GetEntityId() && item.Rect.eulerAngles.z < c_AngleEpsilon;

            return ret;
        }

        public override void OnAnswered(bool answer, bool submit = true)
        {
            m_UnansweredQuestion--;

            if(m_UnansweredQuestion <= 0)
                Submit(c_MaxStar);
        }

        public virtual void Rotate()
        {
            if(m_RotateCo == null)
            {
                m_RotateCo = RotateCo();

                StartCoroutine(m_RotateCo);
            }
        }

        IEnumerator RotateCo()
        {
            float rotateValue = c_QuarterTurn;

            if(m_RotateSound)
                m_Host.AudioSource.PlayOneShot(m_RotateSound);
            
            while (rotateValue > 0)
            {
                foreach (var item in m_Canvas.Items)
                {
                    if(!item.CanBeRotated || item.transform.parent.GetEntityId() != m_ItemsContainer.transform.GetEntityId())
                        continue;

                    item.Rect.localRotation = Quaternion.Euler(Vector3.forward * (item.Rect.eulerAngles.z + c_RotateStepDegrees));

                    if(item.Rect.eulerAngles.z == c_FullCircle)
                        item.Rect.localRotation = Quaternion.Euler(Vector3.zero);
                }

                rotateValue -= c_RotateStepDegrees;

                yield return new WaitForSeconds(Time.deltaTime);
            }

            m_RotateCo = null;
        }

        void SetDialog()
        {
            if (!string.IsNullOrEmpty(m_DataShapeIt.Question.Text))
                m_Host.SetNPCDialog(m_DataShapeIt.Question.Text);
        }
    }
}