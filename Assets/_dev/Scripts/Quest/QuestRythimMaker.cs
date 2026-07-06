using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestRythimMaker : Quest
    {
        [Header("Data")]
        
        [Header("Components")]
        [SerializeField] private Image questionImage;
        [SerializeField] private AudioPlayer questionAudio;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private RectTransform notesContainer;
        [SerializeField] private Collider2D beatButton;

        private SO_QuestRythimMaker dataRythimMaker;
        private QuestRythimNote[] notes;
        private int unansweredNote = 0;
        private int remainingNote = 0;

        protected override void Start()
        {
            base.Start();
            
            dataRythimMaker = data as SO_QuestRythimMaker;

            if(!dataRythimMaker)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestRythimMaker");

            unansweredNote = dataRythimMaker.Items.Length;
            remainingNote = unansweredNote;
            

            if (questionImage)
            {
                if (dataRythimMaker.Question.Image)
                    questionImage.sprite = dataRythimMaker.Question.Image;
                else
                {
                    questionImage.gameObject.SetActive(false);
                    questionImage.transform.parent.gameObject.SetActive(false);
                }
            }

            if (questionAudio)
            {
                if (dataRythimMaker.Question.Audio)
                    questionAudio.SetAudioClip(dataRythimMaker.Question.Audio);
                else
                {
                    questionAudio.gameObject.SetActive(false);
                    questionAudio.transform.parent.gameObject.SetActive(false);
                }
            }

            if (questionText)
            {
                if (!string.IsNullOrEmpty(dataRythimMaker.Question.Text))
                    questionText.text = dataRythimMaker.Question.Text;
                else
                {
                    questionText.gameObject.SetActive(false);
                    questionText.transform.parent.gameObject.SetActive(false);
                }
            }

            if (notesContainer)
            {
                notes = new QuestRythimNote[dataRythimMaker.Items.Length];

                for (int i = 0; i < dataRythimMaker.Items.Length; i++)
                    notes[i] = InstantiateItem(dataRythimMaker.Items[i]);
            }

            SetDialog();

            Invoke("StartPlay", 2);
        }

        QuestRythimNote InstantiateItem(QuestRythimNoteItem item)
        {
            QuestRythimNote note = Instantiate(item.Note, notesContainer);

            note.transform.localPosition = Vector3.zero;
            note.Init();
            note.Setup(item.SpeedModifier * dataRythimMaker.Speed, item.Delay, item.Clip);
            note.gameObject.SetActive(true);

            return note;
        }

        void StartPlay()
        {
            foreach (var note in notes)
                note.Play();
        }

        public override void OnAnswered(bool result, bool submit = false)
        {
            if (result == true)
                unansweredNote--;
            
            remainingNote--;

            if (remainingNote <= 0)
                base.OnAnswered(unansweredNote < Mathf.CeilToInt(notes.Length / 2), true);
        }

        public override void Submit(int star = 1)
        {
            if(unansweredNote <= 1)
                star = 3;
            else if(unansweredNote < Mathf.CeilToInt(notes.Length / 2))
                star = 2;

            GameManager.Instance.Submit(star);
        }

        public override void Reset()
        {
            unansweredNote = dataRythimMaker.Items.Length;
            remainingNote = dataRythimMaker.Items.Length;

            foreach (var choice in notes)
                choice.Reset();

            SetDialog();

            Invoke("StartPlay", 2);
        }

        public override void Enabled()
        {
            base.Enabled();

            SetDialog();
        }

        void SetDialog()
        {
            if (!string.IsNullOrEmpty(dataRythimMaker.Question.Text))
                GameManager.Instance.SetNPCDialog(dataRythimMaker.Question.Text);
        }
    }
}