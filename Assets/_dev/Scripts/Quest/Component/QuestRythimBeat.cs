using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Button))]

    public class QuestRythimBeat : QuestComponent
    {   
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clip;

        private Collider2D col;
        private Animator animator;

        private bool isOnBeat;

        private bool isSuccess;
        
        private float speed = 1;
        private float delay = 0;

        protected override void Awake()
        {
            base.Awake();

            col = GetComponent<Collider2D>();
            animator = GetComponent<Animator>();

            if(!audioSource)
                audioSource = GameManager.Instance.AudioSource;

            if(col)
                col.enabled = false;
            else
                Debug.LogError("Collider not found");

            if(Button)
                Button.onClick.AddListener(Onclick);
            else
                Debug.LogError("Button not found");
        }

        void Onclick()
        {
            if(!isOnBeat)
            {
                isOnBeat = true;

                if(animator)
                    animator.Play("Tap");

                StartCoroutine(BeatCo());
            }
        }

        IEnumerator BeatCo()
        {
            col.enabled = true;

            yield return new WaitForSeconds(0.1f);

            if(clip && !isSuccess)
                audioSource.PlayOneShot(clip);

            yield return new WaitForSeconds(0.1f);

            col.enabled = false;

            isOnBeat = false;

            isSuccess = false;
        }

        void OnTriggerEnter2D(Collider2D collision)
        {
            if(collision.tag == "NoteHead")
            {
                isSuccess = true;
                
                QuestRythimNote note = collision.GetComponent<QuestRythimNote>();
                
                if(note)
                    note.OnBeat(true);
            }
        }
    }
}