using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    [RequireComponent(typeof(Button))]

    public class QuestRythimBeat : QuestComponent
    {   
        private const string c_TapState = "Tap";
        private const float c_TapFeedbackDelay = 0.1f;
        private const string c_NoteHeadTag = "NoteHead";

        [SerializeField] [FormerlySerializedAs("audioSource")] private AudioSource m_AudioSource;
        [SerializeField] [FormerlySerializedAs("clip")] private AudioClip m_Clip;
        
        private Collider2D m_Col;
        private Animator m_Animator;

        private bool m_IsOnBeat;

        private bool m_IsSuccess;
        
        protected override void Awake()
        {
            base.Awake();

            TryGetComponent<Collider2D>(out m_Col);
            m_Animator = GetComponentInChildren<Animator>();

            // m_AudioSource dari host Quest diisi di Start (host belum tersedia saat Awake)


            if(m_Col)
                m_Col.enabled = false;
            else
                Debug.LogError("Collider not found");

            if(Button)
                Button.onClick.AddListener(Onclick);
            else
                Debug.LogError("Button not found");
        }

        void Onclick()
        {
            if(!m_IsOnBeat)
            {
                m_IsOnBeat = true;

                if(m_Animator)
                    m_Animator.Play(c_TapState);

                StartCoroutine(BeatCo());
            }
        }

        IEnumerator BeatCo()
        {
            m_Col.enabled = true;

            yield return new WaitForSeconds(c_TapFeedbackDelay);

            if(m_Clip && !m_IsSuccess)
                m_AudioSource.PlayOneShot(m_Clip);

            yield return new WaitForSeconds(c_TapFeedbackDelay);

            m_Col.enabled = false;

            m_IsOnBeat = false;

            m_IsSuccess = false;
        }

        void OnTriggerEnter2D(Collider2D collision)
        {
            if(collision.tag == c_NoteHeadTag && !m_IsSuccess)
            {
                m_IsSuccess = true;
                
                collision.TryGetComponent<QuestRythimNote>(out QuestRythimNote note);
                
                if(note)
                    note.OnBeat(true);
            }
        }

        protected override void Start()
        {
            base.Start();

            if(!m_AudioSource)
                m_AudioSource = Host.AudioSource;
        }
    }
}