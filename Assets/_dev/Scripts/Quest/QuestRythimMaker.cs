using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestRythimMaker : Quest
    {
        [Header("Data")]
        
        [Header("Components")]
        [SerializeField] [FormerlySerializedAs("questionImage")] private Image m_QuestionImage;
        [SerializeField] [FormerlySerializedAs("questionAudio")] private AudioPlayer m_QuestionAudio;
        [SerializeField] [FormerlySerializedAs("questionText")] private TMP_Text m_QuestionText;
        [SerializeField] [FormerlySerializedAs("notesContainer")] private RectTransform m_NotesContainer;
        [SerializeField] [FormerlySerializedAs("beatButton")] private Collider2D m_BeatButton;

        private SO_QuestRythimMaker m_DataRythimMaker;
        private QuestRythimNote[] m_Notes;
        private int m_UnansweredNote = 0;
        private int m_RemainingNote = 0;

        public static AudioSource AudioSource { get; private set; }

        protected override void Start()
        {
            base.Start();
            
            m_DataRythimMaker = m_Data as SO_QuestRythimMaker;

            AudioSource = GetComponent<AudioSource>();

            if(!AudioSource)
                AudioSource = GameManager.Instance.AudioSource;

            if(!m_DataRythimMaker)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestRythimMaker");

            m_UnansweredNote = m_DataRythimMaker.Items.Length;
            m_RemainingNote = m_UnansweredNote;
            

            if (m_QuestionImage)
            {
                if (m_DataRythimMaker.Question.Image)
                    m_QuestionImage.sprite = m_DataRythimMaker.Question.Image;
                else
                {
                    m_QuestionImage.gameObject.SetActive(false);
                    m_QuestionImage.transform.parent.gameObject.SetActive(false);
                }
            }

            if (m_QuestionAudio)
            {
                if (m_DataRythimMaker.Question.Audio)
                    m_QuestionAudio.SetAudioClip(m_DataRythimMaker.Question.Audio);
                else
                {
                    m_QuestionAudio.gameObject.SetActive(false);
                    m_QuestionAudio.transform.parent.gameObject.SetActive(false);
                }
            }

            if (m_QuestionText)
            {
                if (!string.IsNullOrEmpty(m_DataRythimMaker.Question.Text))
                    m_QuestionText.text = m_DataRythimMaker.Question.Text;
                else
                {
                    m_QuestionText.gameObject.SetActive(false);
                    m_QuestionText.transform.parent.gameObject.SetActive(false);
                }
            }

            if (m_NotesContainer)
            {
                m_Notes = new QuestRythimNote[m_DataRythimMaker.Items.Length];

                for (int i = 0; i < m_DataRythimMaker.Items.Length; i++)
                    m_Notes[i] = InstantiateItem(m_DataRythimMaker.Items[i]);
            }

            SetDialog();

            Invoke("StartPlay", 2);
        }

        QuestRythimNote InstantiateItem(QuestRythimNoteItem item)
        {
            QuestRythimNote note = Instantiate(item.Note, m_NotesContainer);

            note.transform.localPosition = Vector3.zero;
            note.Init();
            note.Setup(item.SpeedModifier * m_DataRythimMaker.Speed, item.Delay, item.Clip);
            note.gameObject.SetActive(true);

            return note;
        }

        void StartPlay()
        {
            foreach (var note in m_Notes)
                note.Play();
        }

        public override void OnAnswered(bool result, bool submit = false)
        {
            if (result == true)
                m_UnansweredNote--;
            
            m_RemainingNote--;

            if (m_RemainingNote <= 0)
                base.OnAnswered(m_UnansweredNote < Mathf.CeilToInt(m_Notes.Length / 2), true);
        }

        public override void Submit(int star = 1)
        {
            if(m_UnansweredNote <= 1)
                star = 3;
            else if(m_UnansweredNote < Mathf.CeilToInt(m_Notes.Length / 2))
                star = 2;

            GameManager.Instance.Submit(star);
        }

        public override void Reset()
        {
            if(m_DataRythimMaker != null)
            {
                m_UnansweredNote = m_DataRythimMaker.Items.Length;
                m_RemainingNote = m_DataRythimMaker.Items.Length;
            }
            
            if(m_Notes != null)
            {
                foreach (var choice in m_Notes)
                    choice.Reset();
            }
            
            SetDialog();

            Invoke("StartPlay", 2);

            base.Reset();
        }

        public override void Enabled()
        {
            base.Enabled();

            SetDialog();
        }

        void SetDialog()
        {
            if (m_DataRythimMaker != null && !string.IsNullOrEmpty(m_DataRythimMaker.Question.Text))
                GameManager.Instance.SetNPCDialog(m_DataRythimMaker.Question.Text);
        }
    }
}